using Aurora.Flowboard.Api.Endpoints;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using TestResult = NetArchTest.Rules.TestResult;

namespace Aurora.Flowboard.ArchitectureTests;

public class ApiLayerTests : BaseTest
{
    private const string EndpointsNamespace = "Aurora.Flowboard.Api.Endpoints";
    private const string MiddlewaresNamespace = "Aurora.Flowboard.Api.Middlewares";
    private const string EndpointTagsTypeName = "Aurora.Flowboard.Api.Endpoints.EndpointTags";
    private const string InfrastructureNamespace = "Aurora.Flowboard.Infrastructure";
    private const string EntityFrameworkCoreNamespace = "Microsoft.EntityFrameworkCore";
    private const string ApplicationDataNamespace = "Aurora.Flowboard.Application.Abstractions.Data";
    private const string ExceptionHandlerInterfaceName = "Microsoft.AspNetCore.Diagnostics.IExceptionHandler";
    private const string RequestSuffix = "Request";
    private const string MapEndpointMethodName = nameof(IBaseEndpoint.MapEndpoint);
    private const string RequireAuthorizationMethodName = "RequireAuthorization";
    private const string AllowAnonymousMethodName = "AllowAnonymous";
    private const string WithNameMethodName = "WithName";
    private const string WithTagsMethodName = "WithTags";
    private const string ProducesMethodName = "Produces";
    private const string DomainNamespacePrefix = "Aurora.Flowboard.Domain.";
    private const string BaseEntityTypeName = "Aurora.Flowboard.Domain.Abstractions.BaseEntity";
    private const string CloneMethodName = "<Clone>$";
    private const char NamespaceSeparator = '.';

    // The only endpoints allowed to opt out of authorization: both are part of the
    // login handshake, so requiring a token on them would be circular.
    private static readonly string[] AnonymousEndpoints = ["Login", "RefreshToken"];

    // Swagger is the contract this API is consumed through, so every route must name
    // itself, carry a tag, and declare at least one response shape.
    private static readonly string[] RequiredOpenApiCalls =
        [WithNameMethodName, WithTagsMethodName, ProducesMethodName];

    [Fact]
    public void Endpoint_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IBaseEndpoint))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoint_Should_BePublic()
    {
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IBaseEndpoint))
            .And()
            .AreNotAbstract()
            .Should()
            .BePublic()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_ShouldNot_DependOnInfrastructure()
    {
        // Api.csproj references Infrastructure so the composition root can wire it up;
        // nothing under Endpoints/ may reach past the Application abstractions.
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith(EndpointsNamespace)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_ShouldNot_DependOnEntityFrameworkCore()
    {
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith(EndpointsNamespace)
            .Should()
            .NotHaveDependencyOnAny(EntityFrameworkCoreNamespace, ApplicationDataNamespace)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_Should_ResideInEndpointsNamespace()
    {
        // BaseEndpointExtensions scans the whole assembly, so an endpoint parked
        // outside Endpoints/ would still be registered and routed.
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IBaseEndpoint))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespaceStartingWith(EndpointsNamespace)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_Should_ResideInNamespaceMatchingAnEndpointTag()
    {
        Type endpointTagsType = ApiAssembly.GetType(EndpointTagsTypeName)
            .ShouldNotBeNull();

        HashSet<string> tags =
        [
            .. endpointTagsType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
        ];

        tags.ShouldNotBeEmpty();

        List<string> failingTypes = [];
        foreach (Type endpointType in GetEndpointTypes())
        {
            string feature = GetFeatureSegment(endpointType);

            if (!tags.Contains(feature))
            {
                failingTypes.Add($"{endpointType.Name} (namespace segment '{feature}' is not declared in EndpointTags)");
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void EndpointRequests_Should_BeNestedInternalSealedRecords()
    {
        IEnumerable<Type> requestTypes = ApiAssembly
            .GetTypes()
            .Where(type => type.Name.EndsWith(RequestSuffix, StringComparison.Ordinal)
                && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

        List<string> failingTypes = [];
        foreach (Type requestType in requestTypes)
        {
            Type? declaringType = requestType.DeclaringType;

            if (declaringType is null || !declaringType.IsAssignableTo(typeof(IBaseEndpoint)))
            {
                failingTypes.Add($"{requestType.Name} (should be nested inside the endpoint that consumes it)");
                continue;
            }

            if (!requestType.IsNestedAssembly)
            {
                failingTypes.Add($"{declaringType.Name}.{requestType.Name} (should be internal)");
            }

            if (!requestType.IsSealed)
            {
                failingTypes.Add($"{declaringType.Name}.{requestType.Name} (should be sealed)");
            }

            if (!IsRecord(requestType))
            {
                failingTypes.Add($"{declaringType.Name}.{requestType.Name} (should be a record)");
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Endpoints_Should_RequireAuthorizationUnlessExplicitlyAnonymous()
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(ApiAssembly.Location);

        List<string> failingEndpoints = [];
        foreach (Type endpointType in GetEndpointTypes())
        {
            HashSet<string>? calledMethods = GetMapEndpointCalls(module, endpointType);

            if (calledMethods is null)
            {
                failingEndpoints.Add($"{endpointType.Name} (no {MapEndpointMethodName} body to inspect)");
                continue;
            }

            bool requiresAuthorization = calledMethods.Contains(RequireAuthorizationMethodName);
            bool isAnonymous = calledMethods.Contains(AllowAnonymousMethodName);

            if (!requiresAuthorization && !isAnonymous)
            {
                failingEndpoints.Add(
                    $"{endpointType.Name} (calls neither {RequireAuthorizationMethodName} nor {AllowAnonymousMethodName})");
            }
            else if (isAnonymous && !IsAllowedToBeAnonymous(endpointType))
            {
                failingEndpoints.Add(
                    $"{endpointType.Name} (calls {AllowAnonymousMethodName} but is not in the anonymous allow list)");
            }
        }

        failingEndpoints.ShouldBeEmpty();
    }

    [Fact]
    public void Endpoints_Should_DeclareOpenApiMetadata()
    {
        using ModuleDefinition module = ModuleDefinition.ReadModule(ApiAssembly.Location);

        List<string> failingEndpoints = [];
        foreach (Type endpointType in GetEndpointTypes())
        {
            HashSet<string>? calledMethods = GetMapEndpointCalls(module, endpointType);

            if (calledMethods is null)
            {
                failingEndpoints.Add($"{endpointType.Name} (no {MapEndpointMethodName} body to inspect)");
                continue;
            }

            failingEndpoints.AddRange(
                RequiredOpenApiCalls
                    .Where(required => !calledMethods.Contains(required))
                    .Select(required => $"{endpointType.Name} (does not call {required})"));
        }

        failingEndpoints.ShouldBeEmpty();
    }

    [Fact]
    public void Endpoints_ShouldNot_DependOnDomainEntities()
    {
        // Enums and value types (ProjectKind, Priority, Role) are part of the wire contract
        // and may be referenced directly. Aggregate roots and their children are not: they
        // stay behind the Application layer, which maps them into response DTOs.
        using ModuleDefinition module = ModuleDefinition.ReadModule(ApiAssembly.Location);

        List<string> failingEndpoints = [];
        foreach (Type endpointType in GetEndpointTypes())
        {
            TypeDefinition? endpointDefinition = module.GetType(endpointType.FullName);

            if (endpointDefinition is null)
            {
                continue;
            }

            failingEndpoints.AddRange(
                GetReferencedDomainEntities(endpointDefinition)
                    .Select(entityName => $"{endpointType.Name} (references the domain entity {entityName})"));
        }

        failingEndpoints.ShouldBeEmpty();
    }

    [Fact]
    public void Middlewares_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith(MiddlewaresNamespace)
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Middlewares_Should_ImplementExceptionHandler()
    {
        IEnumerable<Type> middlewareTypes = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith(MiddlewaresNamespace)
            .And()
            .AreNotAbstract()
            .GetTypes();

        List<string> failingTypes =
        [
            .. middlewareTypes
                .Where(type => !Array.Exists(
                    type.GetInterfaces(),
                    contract => string.Equals(contract.FullName, ExceptionHandlerInterfaceName, StringComparison.Ordinal)))
                .Select(type => $"{type.Name} (should implement {ExceptionHandlerInterfaceName})")
        ];

        failingTypes.ShouldBeEmpty();
    }

    private static HashSet<string>? GetMapEndpointCalls(ModuleDefinition module, Type endpointType)
    {
        MethodDefinition? mapEndpoint = module
            .GetType(endpointType.FullName)?
            .Methods
            .FirstOrDefault(method => method.Name == MapEndpointMethodName && method.HasBody);

        return mapEndpoint is null
            ? null
            : [.. mapEndpoint
                .Body
                .Instructions
                .Select(instruction => instruction.Operand)
                .OfType<MethodReference>()
                .Select(method => method.Name)];
    }

    private static IEnumerable<string> GetReferencedDomainEntities(TypeDefinition endpointDefinition) =>
        GetReferencedTypes(endpointDefinition)
            .SelectMany(Expand)
            .Where(reference => reference.FullName.StartsWith(DomainNamespacePrefix, StringComparison.Ordinal))
            .Select(ResolveDefinition)
            .OfType<TypeDefinition>()
            .Where(InheritsBaseEntity)
            .Select(definition => definition.FullName)
            .Distinct()
            .Order();

    // Walks everything an endpoint can name: its own members, the bodies it compiles to,
    // and the nested request records and lambda display classes Roslyn emits for it.
    private static IEnumerable<TypeReference> GetReferencedTypes(TypeDefinition type)
    {
        foreach (FieldDefinition field in type.Fields)
        {
            yield return field.FieldType;
        }

        foreach (PropertyDefinition property in type.Properties)
        {
            yield return property.PropertyType;
        }

        foreach (MethodDefinition method in type.Methods)
        {
            foreach (TypeReference reference in GetReferencedTypes(method))
            {
                yield return reference;
            }
        }

        foreach (TypeDefinition nested in type.NestedTypes)
        {
            foreach (TypeReference reference in GetReferencedTypes(nested))
            {
                yield return reference;
            }
        }
    }

    private static IEnumerable<TypeReference> GetReferencedTypes(MethodDefinition method)
    {
        yield return method.ReturnType;

        foreach (ParameterDefinition parameter in method.Parameters)
        {
            yield return parameter.ParameterType;
        }

        if (!method.HasBody)
        {
            yield break;
        }

        foreach (VariableDefinition variable in method.Body.Variables)
        {
            yield return variable.VariableType;
        }

        foreach (Instruction instruction in method.Body.Instructions)
        {
            foreach (TypeReference reference in GetReferencedTypes(instruction.Operand))
            {
                yield return reference;
            }
        }
    }

    private static IEnumerable<TypeReference> GetReferencedTypes(object? operand)
    {
        switch (operand)
        {
            case TypeReference typeReference:
                yield return typeReference;
                break;

            case MethodReference methodReference:
                yield return methodReference.DeclaringType;
                yield return methodReference.ReturnType;

                foreach (ParameterDefinition parameter in methodReference.Parameters)
                {
                    yield return parameter.ParameterType;
                }

                break;

            case FieldReference fieldReference:
                yield return fieldReference.DeclaringType;
                yield return fieldReference.FieldType;
                break;

            default:
                break;
        }
    }

    // Unwraps arrays, by-refs and generic instances so IReadOnlyCollection<Project> is
    // not mistaken for a harmless framework type.
    private static IEnumerable<TypeReference> Expand(TypeReference reference)
    {
        yield return reference;

        if (reference is GenericInstanceType generic)
        {
            foreach (TypeReference argument in generic.GenericArguments.SelectMany(Expand))
            {
                yield return argument;
            }
        }

        if (reference is TypeSpecification specification && specification.ElementType != reference)
        {
            foreach (TypeReference element in Expand(specification.ElementType))
            {
                yield return element;
            }
        }
    }

    private static TypeDefinition? ResolveDefinition(TypeReference reference)
    {
        try
        {
            return reference.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }

    private static bool InheritsBaseEntity(TypeDefinition type)
    {
        for (TypeReference? current = type.BaseType;
            current is not null;
            current = ResolveDefinition(current)?.BaseType)
        {
            if (string.Equals(current.FullName, BaseEntityTypeName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Type> GetEndpointTypes() =>
        Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IBaseEndpoint))
            .And()
            .AreNotAbstract()
            .GetTypes();

    private static string GetFeatureSegment(Type endpointType)
    {
        string endpointNamespace = endpointType.Namespace ?? string.Empty;
        int separatorIndex = endpointNamespace.LastIndexOf(NamespaceSeparator);

        return separatorIndex < 0
            ? endpointNamespace
            : endpointNamespace[(separatorIndex + 1)..];
    }

    private static bool IsAllowedToBeAnonymous(Type endpointType) =>
        Array.Exists(
            AnonymousEndpoints,
            name => string.Equals(name, endpointType.Name, StringComparison.Ordinal));

    private static bool IsRecord(Type type) =>
        type.GetMethod(CloneMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
