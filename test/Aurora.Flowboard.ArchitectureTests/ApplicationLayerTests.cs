using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.Abstractions.Pagination;
using FluentValidation;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using TestResult = NetArchTest.Rules.TestResult;

namespace Aurora.Flowboard.ArchitectureTests;

public class ApplicationLayerTests : BaseTest
{
    private const string BehaviorsNamespace = "Aurora.Flowboard.Application.Abstractions.Behaviors";
    private const string AspNetCoreNamespace = "Microsoft.AspNetCore";
    private const string BehaviorSuffix = "Behavior";
    private const string ResponseSuffix = "Response";
    private const char GenericAritySeparator = '`';
    private const string CloneMethodName = "<Clone>$";
    private const string NowPropertyName = "Now";
    private const string UtcNowPropertyName = "UtcNow";
    private const string TodayPropertyName = "Today";
    private const string GetterPrefix = "get_";
    private const byte CallOpCode = 0x28;
    private const byte CallVirtOpCode = 0x6F;
    private const int TokenSize = 4;

    [Fact]
    public void Command_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandWithResponse_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand<>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Command_Should_HaveCommandSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandWithResponse_Should_HaveCommandSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand<>))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandler_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandlerWithResponse_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandler_ShouldNot_BePublic()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandlerWithResponse_ShouldNot_BePublic()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandler_Should_HaveHandlerSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespace(BehaviorsNamespace)
            .Should()
            .HaveNameEndingWith("Handler")
            .Or()
            .HaveNameEndingWith("Handler`1")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void CommandHandlerWithResponse_Should_HaveHandlerSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespace(BehaviorsNamespace)
            .Should()
            .HaveNameEndingWith("Handler")
            .Or()
            .HaveNameEndingWith("Handler`1")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Query_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Query_Should_HaveQuerySuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void QueryHandler_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void QueryHandler_ShouldNot_BePublic()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void QueryHandler_Should_HaveHandlerSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And()
            .AreNotAbstract()
            .And()
            .DoNotResideInNamespace(BehaviorsNamespace)
            .Should()
            .HaveNameEndingWith("Handler")
            .Or()
            .HaveNameEndingWith("Handler`1")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Validator_Should_HaveValidatorSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Validator_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Validator_ShouldNot_BePublic()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventHandlers_Should_BeSealed()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventHandlers_Should_HaveEventHandlerSuffix()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("EventHandler")
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Commands_Should_BeRecords()
    {
        // NetArchTest's BeImmutable() cannot be used here: `init` accessors compile to
        // non-readonly backing fields, so every record would be reported as mutable.
        List<string> failingTypes =
        [
            .. GetApplicationTypes()
                .Where(t => !t.IsAbstract && IsCommand(t) && !IsRecord(t))
                .Select(t => t.Name)
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Queries_Should_BeRecords()
    {
        List<string> failingTypes =
        [
            .. GetApplicationTypes()
                .Where(t => !t.IsAbstract && IsQuery(t) && !IsRecord(t))
                .Select(t => t.Name)
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Commands_Should_HaveValidator()
    {
        HashSet<Type> validatedTypes = GetValidatedTypes();

        List<string> failingTypes =
        [
            .. GetApplicationTypes()
                .Where(t => !t.IsAbstract && IsCommand(t) && !validatedTypes.Contains(t))
                .Select(t => t.Name)
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Queries_Should_HaveValidator()
    {
        HashSet<Type> validatedTypes = GetValidatedTypes();

        // Parameterless queries (GetAllProjectsQuery, GetAllUsersQuery) carry no input
        // to validate, so they are excluded rather than forced to own an empty validator.
        List<string> failingTypes =
        [
            .. GetApplicationTypes()
                .Where(t => !t.IsAbstract
                    && IsQuery(t)
                    && HasPublicProperties(t)
                    && !validatedTypes.Contains(t))
                .Select(t => t.Name)
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Handler_Should_ResideInSameNamespaceAs_ItsMessage()
    {
        List<string> failingTypes = [];
        foreach (Type handlerType in GetHandlerTypes())
        {
            Type? messageType = GetHandledMessageType(handlerType);

            if (messageType is null || messageType.IsGenericParameter)
            {
                continue;
            }

            if (!string.Equals(handlerType.Namespace, messageType.Namespace, StringComparison.Ordinal))
            {
                failingTypes.Add(
                    $"{handlerType.Name} ({handlerType.Namespace}) should live next to {messageType.Name} ({messageType.Namespace})");
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Application_ShouldNot_DependOn_AspNetCore()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(AspNetCoreNamespace)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Handlers_ShouldNot_UseAmbientSystemClock()
    {
        // The current time must arrive through IDateTimeProvider so handlers stay testable.
        List<string> failingMembers = [];
        foreach (Type handlerType in GetHandlerTypes())
        {
            foreach (MethodBase method in GetMethodsIncludingNested(handlerType))
            {
                if (UsesAmbientClock(method))
                {
                    failingMembers.Add($"{handlerType.Name}.{method.Name}");
                }
            }
        }

        failingMembers.ShouldBeEmpty();
    }

    [Fact]
    public void Handlers_AsyncMethods_Should_HaveCancellationTokenParameter()
    {
        List<string> failingMembers = [];
        foreach (Type handlerType in GetHandlerTypes())
        {
            IEnumerable<MethodInfo> awaitableMethods = handlerType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                    && IsAwaitable(m.ReturnType));

            foreach (MethodInfo method in awaitableMethods)
            {
                if (!Array.Exists(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken)))
                {
                    failingMembers.Add($"{handlerType.Name}.{method.Name}");
                }
            }
        }

        failingMembers.ShouldBeEmpty();
    }

    [Fact]
    public void Responses_Should_BeSealedRecords()
    {
        List<string> failingTypes = [];
        foreach (Type responseType in GetResponseTypes())
        {
            if (!responseType.IsSealed)
            {
                failingTypes.Add($"{GetSimpleName(responseType)} (should be sealed)");
            }

            if (!IsRecord(responseType))
            {
                failingTypes.Add($"{GetSimpleName(responseType)} (should be a record)");
            }
        }

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void QueryResults_Should_HaveResponseSuffix()
    {
        // Commands are exempt on purpose: they return ids or tokens (Guid, IdentityToken),
        // not read models. Only query results are part of the response contract.
        List<string> failingTypes =
        [
            .. GetQueryResultTypes()
                .Where(t => !GetSimpleName(t).EndsWith(ResponseSuffix, StringComparison.Ordinal))
                .Select(t => $"{GetSimpleName(t)} (query result should have the '{ResponseSuffix}' suffix)")
        ];

        failingTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Behaviors_Should_ResideInBehaviorsNamespace()
    {
        List<string> failingTypes =
        [
            // A handler that wraps another handler is a pipeline decorator.
            .. GetApplicationTypes()
                .Where(t => IsHandler(t) && WrapsAnotherHandler(t) && !ResidesInBehaviorsNamespace(t))
                .Select(t => $"{t.Name} (decorator outside {BehaviorsNamespace})"),
            .. GetApplicationTypes()
                .Where(t => t.Name.EndsWith(BehaviorSuffix, StringComparison.Ordinal)
                    && !ResidesInBehaviorsNamespace(t))
                .Select(t => $"{t.Name} (named '*{BehaviorSuffix}' outside {BehaviorsNamespace})")
        ];

        failingTypes.ShouldBeEmpty();
    }

    private static IEnumerable<Type> GetApplicationTypes() =>
        ApplicationAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

    // Abstract handlers are included on purpose: WorkItemFieldUpdateHandler holds the
    // body shared by eight concrete handlers, so it must obey the same rules.
    private static IEnumerable<Type> GetHandlerTypes() =>
        GetApplicationTypes().Where(IsHandler);

    private static IEnumerable<Type> GetResponseTypes() =>
        GetApplicationTypes()
            .Where(t => GetSimpleName(t).EndsWith(ResponseSuffix, StringComparison.Ordinal));

    // Unwraps the collection and paging envelopes so the element type is the one checked.
    private static IEnumerable<Type> GetQueryResultTypes() =>
        GetApplicationTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => IsGenericInterface(i, typeof(IQuery<>)))
            .Select(i => UnwrapResult(i.GetGenericArguments()[0]))
            .Where(t => !t.IsGenericParameter && t.Assembly == ApplicationAssembly)
            .Distinct();

    private static Type UnwrapResult(Type resultType)
    {
        if (!resultType.IsGenericType)
        {
            return resultType;
        }

        Type definition = resultType.GetGenericTypeDefinition();

        return definition == typeof(IReadOnlyCollection<>) || definition == typeof(PagedResponse<>)
            ? UnwrapResult(resultType.GetGenericArguments()[0])
            : resultType;
    }

    private static string GetSimpleName(Type type)
    {
        int arityIndex = type.Name.IndexOf(GenericAritySeparator);

        return arityIndex < 0 ? type.Name : type.Name[..arityIndex];
    }

    private static bool IsCommand(Type type) =>
        typeof(ICommand).IsAssignableFrom(type)
            || Array.Exists(type.GetInterfaces(), i => IsGenericInterface(i, typeof(ICommand<>)));

    private static bool IsQuery(Type type) =>
        Array.Exists(type.GetInterfaces(), i => IsGenericInterface(i, typeof(IQuery<>)));

    private static bool IsHandler(Type type) => GetHandlerInterface(type) is not null;

    private static Type? GetHandlerInterface(Type type) =>
        Array.Find(
            type.GetInterfaces(),
            i => IsGenericInterface(i, typeof(ICommandHandler<>))
                || IsGenericInterface(i, typeof(ICommandHandler<,>))
                || IsGenericInterface(i, typeof(IQueryHandler<,>)));

    private static Type? GetHandledMessageType(Type type) =>
        GetHandlerInterface(type)?.GetGenericArguments()[0];

    private static bool WrapsAnotherHandler(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SelectMany(c => c.GetParameters())
            .Any(p => IsGenericInterface(p.ParameterType, typeof(ICommandHandler<>))
                || IsGenericInterface(p.ParameterType, typeof(ICommandHandler<,>))
                || IsGenericInterface(p.ParameterType, typeof(IQueryHandler<,>)));

    private static bool ResidesInBehaviorsNamespace(Type type) =>
        type.Namespace?.StartsWith(BehaviorsNamespace, StringComparison.Ordinal) == true;

    private static bool IsGenericInterface(Type candidate, Type openGenericInterface) =>
        candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface;

    private static bool IsRecord(Type type) =>
        type.GetMethod(CloneMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static bool HasPublicProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0;

    private static HashSet<Type> GetValidatedTypes() =>
        [.. GetApplicationTypes().Select(GetValidatedType).OfType<Type>()];

    private static Type? GetValidatedType(Type validatorType)
    {
        for (Type? current = validatorType.BaseType; current is not null; current = current.BaseType)
        {
            if (IsGenericInterface(current, typeof(AbstractValidator<>)))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static bool IsAwaitable(Type returnType) =>
        returnType == typeof(Task)
            || returnType == typeof(ValueTask)
            || (returnType.IsGenericType
                && (returnType.GetGenericTypeDefinition() == typeof(Task<>)
                    || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)));

    private static IEnumerable<MethodBase> GetMethodsIncludingNested(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (MethodBase method in type.GetMethods(flags))
        {
            yield return method;
        }

        // async bodies and lambdas live in compiler-generated nested state machines.
        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (MethodBase method in GetMethodsIncludingNested(nested))
            {
                yield return method;
            }
        }
    }

    private static bool UsesAmbientClock(MethodBase method)
    {
        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();

        if (il is null)
        {
            return false;
        }

        Type[] typeArguments = method.DeclaringType?.IsGenericType == true
            ? method.DeclaringType.GetGenericArguments()
            : Type.EmptyTypes;

        for (int i = 0; i + TokenSize < il.Length; i++)
        {
            if (il[i] != CallOpCode && il[i] != CallVirtOpCode)
            {
                continue;
            }

            if (IsAmbientClockAccessor(ResolveCallee(method, il, i, typeArguments)))
            {
                return true;
            }
        }

        return false;
    }

    private static MethodBase? ResolveCallee(MethodBase method, byte[] il, int offset, Type[] typeArguments)
    {
        try
        {
            return method.Module.ResolveMethod(
                BitConverter.ToInt32(il, offset + 1),
                typeArguments,
                Type.EmptyTypes);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsAmbientClockAccessor(MethodBase? callee) =>
        (callee?.DeclaringType == typeof(DateTime) || callee?.DeclaringType == typeof(DateTimeOffset))
            && (string.Equals(callee.Name, GetterPrefix + NowPropertyName, StringComparison.Ordinal)
                || string.Equals(callee.Name, GetterPrefix + UtcNowPropertyName, StringComparison.Ordinal)
                || string.Equals(callee.Name, GetterPrefix + TodayPropertyName, StringComparison.Ordinal));
}
