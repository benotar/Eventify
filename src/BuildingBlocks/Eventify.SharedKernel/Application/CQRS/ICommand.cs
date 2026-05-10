using ErrorOr;
using MediatR;

namespace Eventify.SharedKernel.Application.CQRS;

public interface ICommand : IRequest<ErrorOr<Success>>;

public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>>;
