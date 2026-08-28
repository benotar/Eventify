using Eventify.ArchitectureTests.Commons;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using Shouldly;

namespace Eventify.Catalog.ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
    [Fact]
    public void Domain_Should_NotHaveDependencyOnApplication()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(DomainAssembly, ApplicationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnInfrastructure()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(DomainAssembly, InfrastructureAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(DomainAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOnInfrastructure()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(ApplicationAssembly, InfrastructureAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(ApplicationAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureChecker.GetNoDependencyOnResult(InfrastructureAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_Should_NotInheritController()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .ShouldNot()
            .Inherit(typeof(ControllerBase))
            .Or()
            .Inherit(typeof(Controller))
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"Failing types: {string.Join(", ",
            result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }
}
