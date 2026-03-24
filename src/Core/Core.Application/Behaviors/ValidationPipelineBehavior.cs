using Core.Application.Infrastructure;
using Core.Shared.Errors;
using Core.Shared.Results;
using FluentValidation;
using MediatR;

namespace Core.Application.Behaviors;

public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var errors = await ValidateAsync(context, cancellationToken);

        if (errors.Length > 0)
        {
            return ValidationResultFactory.Create<TResponse>(errors);
        }

        return await next();
    }

    private async Task<Error[]> ValidateAsync(ValidationContext<TRequest> context, CancellationToken cancellationToken)
    {
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        return validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
            .Distinct()
            .ToArray();
    }
}