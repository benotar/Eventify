using ErrorOr;
using MediatR;

namespace Eventify.SharedKernel.Application.CQRS;

public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>>;
