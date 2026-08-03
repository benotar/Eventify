using ErrorOr;
using MediatR;

namespace Eventify.SharedKernel.Application.CQRS;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, ErrorOr<Success>>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, ErrorOr<TResponse>>
    where TCommand : ICommand<TResponse>;
