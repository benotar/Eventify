using MediatR;

namespace Eventify.SharedKernel.Application.CQRS;

public interface ICommand : IRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>;
