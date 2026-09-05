using System.Reflection;
using Carter;
using Eventify.ArchitectureTests.Commons;
using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.ArchitectureTests.Conventions;

public class CommonNamingConventionsTests : BaseTest
{
    [Fact]
    public void CommandHandlers_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(ICommandHandler<>),
            "CommandHandler");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void CommandHandlersGeneric_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(ICommandHandler<,>),
            "CommandHandler");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void QueriesHandlers_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(IQueryHandler<,>),
            "QueryHandler");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void Commands_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(ICommand),
            "Command");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void CommandsGeneric_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(ICommand<>),
            "Command");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void Queries_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetHandlerFollowTheNameConventionResult(ApplicationAssembly, typeof(IQuery<>), "Query");

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void Interfaces_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetInterfacesFollowTheNameConventionResult([
            DomainAssembly, ApplicationAssembly, InfrastructureAssembly, PresentationAssembly
        ]);

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void Endpoints_Should_FollowTheNameConvention()
    {
        var result = ArchitectureHelper.GetEndpointsFollowTheNameConventionResult(PresentationAssembly, typeof(ICarterModule));

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void MethodsNameEndsAsAsync_Should_ReturnCorrectType()
    {
        var invalidAsyncMethods = new List<Assembly> { ApplicationAssembly, InfrastructureAssembly, PresentationAssembly }
            .GetMethodsByPredicate(method => method.IsAsyncMethodWithoutAsyncSuffix())
            .ToInvalidNames();

        invalidAsyncMethods.ShouldBeEmptyAndEnd("Async");
    }
}
