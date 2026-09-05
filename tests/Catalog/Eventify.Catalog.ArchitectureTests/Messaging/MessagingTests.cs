using Eventify.ArchitectureTests.Commons;
using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.ArchitectureTests.Messaging;

public class MessagingTests : BaseTest
{
    [Fact]
    public void CommandHandlers_Should_ImplementICommandHandler()
    {
        var commandHandlerGenericType = typeof(ICommandHandler<>);
        var commandHandlerGeneric2Type = typeof(ICommandHandler<,>);

        var types = ApplicationAssembly.GetNotImplementedInterface("CommandHandler",
                commandHandlerGenericType, commandHandlerGeneric2Type)
            .ToInvalidNames();

        types.ShouldBeEmptyAndImplement("CommandHandlers", commandHandlerGenericType, commandHandlerGeneric2Type);
    }

    [Fact]
    public void QueryHandlers_Should_ImplementIQueryHandler()
    {
        var queryHandlerGeneric2Type = typeof(IQueryHandler<,>);

        var types = ApplicationAssembly.GetNotImplementedInterface("QueryHandler",
                queryHandlerGeneric2Type)
            .ToInvalidNames();

        types.ShouldBeEmptyAndImplement("QueryHandlers", queryHandlerGeneric2Type);
    }

    [Fact]
    public void Commands_Should_ImplementICommand()
    {
        var commandType = typeof(ICommand);
        var commandGenericType = typeof(ICommand<>);

        var types = ApplicationAssembly.GetNotImplementedInterface("Command",
                commandType, commandGenericType)
            .ToInvalidNames();

        types.ShouldBeEmptyAndImplement("Commands", commandType, commandGenericType);
    }

    [Fact]
    public void Queries_Should_ImplementIQuery()
    {
        var queryGenericType = typeof(IQuery<>);

        var types = ApplicationAssembly.GetNotImplementedInterface("Query",
                queryGenericType)
            .ToInvalidNames();

        types.ShouldBeEmptyAndImplement("Queries", queryGenericType);
    }

    [Fact]
    public void ImplementedCommandHandlers_Should_HaveCorrectName()
    {
        var result = ArchitectureHelper.GetTypesThatImplementInterfaceHaveNameEndingResult(ApplicationAssembly, "CommandHandler",
            typeof(ICommandHandler<>), typeof(ICommandHandler<,>));

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void ImplementedQueryHandlers_Should_HaveCorrectName()
    {
        var result = ArchitectureHelper.GetTypesThatImplementInterfaceHaveNameEndingResult(ApplicationAssembly, "QueryHandler",
            typeof(IQueryHandler<,>));

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void ImplementedCommands_Should_HaveCorrectName()
    {
        var result = ArchitectureHelper.GetTypesThatImplementInterfaceHaveNameEndingResult(ApplicationAssembly, "Command",
            typeof(ICommand), typeof(ICommand<>));

        result.ShouldBeSuccessful();
    }

    [Fact]
    public void ImplementedQueriesHandlers_Should_HaveCorrectName()
    {
        var result = ArchitectureHelper.GetTypesThatImplementInterfaceHaveNameEndingResult(ApplicationAssembly, "Query",
            typeof(IQuery<>));

        result.ShouldBeSuccessful();
    }
}
