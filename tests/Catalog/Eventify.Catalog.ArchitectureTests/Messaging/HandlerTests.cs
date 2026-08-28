using Eventify.ArchitectureTests.Commons;
using Eventify.SharedKernel.Application.Messaging;
using Shouldly;

namespace Eventify.Catalog.ArchitectureTests.Messaging;

public class HandlerTests : BaseTest
{
    private const string ErrorMessage = "Failing types: {0}";

    [Fact]
    public void CommandHandlers_Should_ImplementICommandHandler_WhenWithoutResponse()
    {
        var result = ArchitectureChecker.GetHandlerImplementInterfaceResult(ApplicationAssembly, typeof(ICommandHandler<>),
            "CommandHandler");

        result.IsSuccessful.ShouldBeTrue($"Failing types: {string.Join(", ",
            result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact]
    public void CommandHandlers_Should_ImplementICommandHandler_WhenWithResponse()
    {
        var result =
            ArchitectureChecker.GetHandlerImplementInterfaceResult(ApplicationAssembly, typeof(ICommandHandler<,>),
                "CommandHandler");

        result.IsSuccessful.ShouldBeTrue($"Failing types: {string.Join(", ",
            result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact]
    public void QueryHandlers_Should_ImplementIQueryHandler()
    {
        var result =
            ArchitectureChecker.GetHandlerImplementInterfaceResult(ApplicationAssembly, typeof(IQueryHandler<,>), "QueryHandler");

        result.IsSuccessful.ShouldBeTrue($"Failing types: {string.Join(", ",
            result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }
}
