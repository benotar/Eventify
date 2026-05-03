using MediatR;

namespace Eventify.SharedKernel.Application.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>;
