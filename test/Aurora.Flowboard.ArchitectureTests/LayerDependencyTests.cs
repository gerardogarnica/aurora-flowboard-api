using NetArchTest.Rules;
using Shouldly;
using TestResult = NetArchTest.Rules.TestResult;

namespace Aurora.Flowboard.ArchitectureTests;

public class LayerDependencyTests : BaseTest
{
    private const string ApplicationNamespacePrefix = "Aurora.Flowboard.Application.";
    private const string ApplicationAbstractionsNamespace = "Aurora.Flowboard.Application.Abstractions";

    [Fact]
    public void Domain_Should_NotHaveDependencyOn_ApplicationLayer()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOn_InfrastructureLayer()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOn_ApiLayer()
    {
        TestResult testResult = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOn_InfrastructureLayer()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOn_ApiLayer()
    {
        TestResult testResult = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_Should_NotHaveDependencyOn_ApiLayer()
    {
        TestResult testResult = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_Should_NotHaveDependencyOn_ApplicationSlices()
    {
        // Infrastructure implements the ports declared in Application.Abstractions
        // (IApplicationDbContext, ITokenProvider, IDateTimeProvider). Reaching into a
        // concrete slice would invert the dependency and couple persistence to a use case.
        string[] sliceNamespaces = GetApplicationSliceNamespaces();

        sliceNamespaces.ShouldNotBeEmpty();

        TestResult testResult = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(sliceNamespaces)
            .GetResult();

        testResult.IsSuccessful.ShouldBeTrue();
    }

    private static string[] GetApplicationSliceNamespaces() =>
        [.. ApplicationAssembly
            .GetTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Where(ns => ns.StartsWith(ApplicationNamespacePrefix, StringComparison.Ordinal)
                && !ns.StartsWith(ApplicationAbstractionsNamespace, StringComparison.Ordinal))
            .Distinct()];
}
