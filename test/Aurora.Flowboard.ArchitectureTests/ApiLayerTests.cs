using Aurora.Flowboard.Api.Endpoints;
using Mono.Cecil;
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
    private const string CloneMethodName = "<Clone>$";
    private const char NamespaceSeparator = '.';

    // The only endpoints allowed to opt out of authorization: both are part of the
    // login handshake, so requiring a token on them would be circular.
    private static readonly string[] AnonymousEndpoints = ["Login", "RefreshToken"];

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
            MethodDefinition? mapEndpoint = module
                .GetType(endpointType.FullName)?
                .Methods
                .FirstOrDefault(method => method.Name == MapEndpointMethodName && method.HasBody);

            if (mapEndpoint is null)
            {
                failingEndpoints.Add($"{endpointType.Name} (no {MapEndpointMethodName} body to inspect)");
                continue;
            }

            HashSet<string> calledMethods =
            [
                .. mapEndpoint
                    .Body
                    .Instructions
                    .Select(instruction => instruction.Operand)
                    .OfType<MethodReference>()
                    .Select(method => method.Name)
            ];

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
