using Faster.Modulith.Contracts;

namespace Faster.Modulith.Behaviors;

/// <summary>
/// Validates requests using FluentValidation before execution
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<FluentValidation.IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<FluentValidation.IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (_validators.Any())
        {
            var context = new FluentValidation.ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
                throw new FluentValidation.ValidationException(failures);
        }

        return await next();
    }
}