using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.SharedKernel.Application.Behaviors;

// TODO Implement
static internal class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

// private readonly IEnumerable<IValidator<TRequest>> _validators;
    //
    // public ValidationDecorator(IEnumerable<IValidator<TRequest>> validators)
    // {
    //     _validators = validators;
    // }
    //
    // public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
    //     CancellationToken cancellationToken)
    // {
    //     if (_validators.IsEmpty)
    //     {
    //         return await next();
    //     }
    //
    //     var context = new ValidationContext<TRequest>(request);
    //
    //     var validationResults = await Task.WhenAll(_validators.Select(v =>
    //         v.ValidateAsync(context, cancellationToken)));
    //
    //     var errors = validationResults
    //         .SelectMany(r => r.Errors)
    //         .Where(f => f is not null)
    //         .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage, f.FormattedMessagePlaceholderValues))
    //         .ToList();
    //
    //     if (errors.IsEmpty)
    //     {
    //         return await next();
    //     }
    //
    //     return (TResponse)(dynamic)errors;
    // }
}
