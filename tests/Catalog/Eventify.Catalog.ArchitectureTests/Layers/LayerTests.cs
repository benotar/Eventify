using Eventify.ArchitectureTests.Commons;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Eventify.Catalog.ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
    [Fact]
    public void Domain_Should_NotHaveDependencyOnApplication()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(DomainAssembly, ApplicationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnInfrastructure()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(DomainAssembly, InfrastructureAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(DomainAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOnInfrastructure()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(ApplicationAssembly, InfrastructureAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(ApplicationAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_Should_NotHaveDependencyOnPresentation()
    {
        var result = ArchitectureHelper.GetNoDependencyOnResult(InfrastructureAssembly, PresentationAssembly);

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Presentation_Should_NotInheritController()
    {
        var result = ArchitectureHelper.GetAssemblyNotInheritTypeResult(PresentationAssembly, typeof(ControllerBase),
            typeof(Controller));

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void ApiEndpoints_Should_NotHaveDependencyOnEfCore()
    {
        var result = ArchitectureHelper.GetAssemblyInNamespaceNoDependencyOnResult(PresentationAssembly, "Catalog.Api.Endpoints",
            "Microsoft.EntityFrameworkCore");

        result.ShouldBeSuccessful();
    }
}
