using Aurora.Flowboard.Domain.Abstractions;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using TestResult = NetArchTest.Rules.TestResult;

namespace Aurora.Flowboard.ArchitectureTests;

public class DomainLayerTests : BaseTest
{
    private const string AbstractionsNamespace = "Aurora.Flowboard.Domain.Abstractions";
    private const string SharedNamespace = "Aurora.Flowboard.Domain.Shared";
    private const string SystemAssemblyPrefix = "System.";
    private const string EventsNamespaceSuffix = ".Events";
    private const string ErrorsSuffix = "Errors";
    private const string CreateMethodName = "Create";
    private const string CloneMethodName = "<Clone>$";
    private const string InitOnlyMarker = "System.Runtime.CompilerServices.IsExternalInit";

    [Fact]
    public void Domain_Should_OnlyReference_FrameworkAssemblies()
    {
        // The domain project carries no PackageReference at all: it must stay free of
        // EF Core, FluentValidation, ASP.NET and anything else outside the BCL.
        List<string> failingReferences =
        [
            .. DomainAssembly
                .GetReferencedAssemblies()
                .Select(assembly => assembly.Name ?? string.Empty)
                .Where(name => !name.StartsWith(SystemAssemblyPrefix, StringComparison.Ordinal))
        ];

        failingReferences.ShouldBeEmpty();
    }

    [Fact]
    public void BaseEntities_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void BaseEntities_ShouldHave_PrivateParameterlessConstructor()
    {
        IEnumerable<Type> entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .And()
            .AreNotAbstract()
            .GetTypes();

        List<Type> failingTypes = [];
        foreach (Type entityType in entityTypes)
        {
            ConstructorInfo[] constructors = entityType.GetConstructors(BindingFlags.NonPublic |
                                                                        BindingFlags.Instance);

            if (!constructors.Any(c => c.IsPrivate && c.GetParameters().Length == 0))
            {
                failingTypes.Add(entityType);
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void BaseEntities_ShouldHave_StaticCreateMethod()
    {
        IEnumerable<Type> entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .And()
            .AreNotAbstract()
            .GetTypes();

        List<Type> failingTypes = [];
        foreach (Type entityType in entityTypes)
        {
            MethodInfo? createMethod = entityType.GetMethod(
                CreateMethodName,
                BindingFlags.Public | BindingFlags.Static);

            if (createMethod == null)
            {
                failingTypes.Add(entityType);
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void BaseEntities_ShouldNot_HavePublicSetters()
    {
        IEnumerable<Type> entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .And()
            .AreNotAbstract()
            .GetTypes();

        List<string> failingMembers = [];
        foreach (Type entityType in entityTypes)
        {
            IEnumerable<PropertyInfo> properties = entityType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                MethodInfo? setter = property.SetMethod;

                if (setter?.IsPublic == true && !IsInitOnly(setter))
                {
                    failingMembers.Add($"{entityType.Name}.{property.Name}");
                }
            }
        }

        failingMembers.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainEvents_Should_HaveDomainEventSuffix()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith(nameof(DomainEvent))
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainEvents_Should_InheritDomainEvent()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(DomainEvent))
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainEvents_Should_ResideInEventsNamespace()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespaceEndingWith(EventsNamespaceSuffix)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Errors_Should_BeStaticAndHaveErrorsSuffix()
    {
        List<Type> publicClasses = [.. GetDomainTypes().Where(t => t.IsClass)];

        List<string> failingTypes =
        [
            .. publicClasses
                .Where(t => t.Name.EndsWith(ErrorsSuffix, StringComparison.Ordinal) && !IsStaticClass(t))
                .Select(t => $"{t.Name} (should be static)"),
            .. publicClasses
                .Where(t => IsStaticClass(t) && !t.Name.EndsWith(ErrorsSuffix, StringComparison.Ordinal))
                .Select(t => $"{t.Name} (static class should have the '{ErrorsSuffix}' suffix)")
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Errors_Should_ExposeOnlyBaseErrorFields()
    {
        IEnumerable<Type> errorTypes = GetDomainTypes()
            .Where(t => IsStaticClass(t) && t.Name.EndsWith(ErrorsSuffix, StringComparison.Ordinal));

        List<string> failingMembers = [];
        foreach (Type errorType in errorTypes)
        {
            string expectedPrefix = string.Concat(
                errorType.Name.AsSpan(0, errorType.Name.Length - ErrorsSuffix.Length),
                ".");

            MemberInfo[] members = errorType.GetMembers(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (MemberInfo member in members)
            {
                if (member is not FieldInfo field)
                {
                    failingMembers.Add($"{errorType.Name}.{member.Name} (only static readonly BaseError fields are allowed)");
                    continue;
                }

                if (!field.IsInitOnly || field.FieldType != typeof(BaseError))
                {
                    failingMembers.Add($"{errorType.Name}.{field.Name} (should be a static readonly BaseError)");
                    continue;
                }

                if (field.GetValue(null) is BaseError error
                    && !error.Code.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    failingMembers.Add($"{errorType.Name}.{field.Name} (code '{error.Code}' should start with '{expectedPrefix}')");
                }
            }
        }

        failingMembers.ShouldBeEmpty();
    }

    [Fact]
    public void ValueObjects_Should_BeSealedRecordsWithPrivateConstructorAndCreateFactory()
    {
        IEnumerable<Type> valueObjectTypes = GetDomainTypes()
            .Where(t => t.Namespace?.StartsWith(AbstractionsNamespace, StringComparison.Ordinal) != true)
            .Where(IsRecord);

        List<string> failingTypes = [];
        foreach (Type valueObjectType in valueObjectTypes)
        {
            if (!valueObjectType.IsSealed)
            {
                failingTypes.Add($"{valueObjectType.Name} (should be sealed)");
            }

            ConstructorInfo[] constructors = valueObjectType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (Array.Exists(constructors, c => c.IsPublic))
            {
                failingTypes.Add($"{valueObjectType.Name} (should not expose a public constructor)");
            }

            MethodInfo? createMethod = valueObjectType.GetMethod(
                CreateMethodName,
                BindingFlags.Public | BindingFlags.Static);

            if (createMethod is null || !ReturnsResultOf(createMethod, valueObjectType))
            {
                failingTypes.Add($"{valueObjectType.Name} (should expose a public static Create returning Result<{valueObjectType.Name}>)");
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Enums_ShouldNot_ResideInSharedNamespace()
    {
        List<string> failingTypes =
        [
            .. GetDomainTypes()
                .Where(t => t.IsEnum && string.Equals(t.Namespace, SharedNamespace, StringComparison.Ordinal))
                .Select(t => t.Name)
        ];

        failingTypes.ShouldBeEmpty();
    }

    private static IEnumerable<Type> GetDomainTypes() =>
        DomainAssembly
            .GetTypes()
            .Where(t => t.IsPublic
                && !t.IsNested
                && !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

    private static bool IsStaticClass(Type type) => type is { IsClass: true, IsAbstract: true, IsSealed: true };

    private static bool IsRecord(Type type) =>
        type.GetMethod(CloneMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static bool IsInitOnly(MethodInfo setter) =>
        Array.Exists(
            setter.ReturnParameter.GetRequiredCustomModifiers(),
            modifier => string.Equals(modifier.FullName, InitOnlyMarker, StringComparison.Ordinal));

    private static bool ReturnsResultOf(MethodInfo method, Type valueObjectType) =>
        method.ReturnType.IsGenericType
            && method.ReturnType.GetGenericTypeDefinition() == typeof(Result<>)
            && method.ReturnType.GetGenericArguments()[0] == valueObjectType;
}
