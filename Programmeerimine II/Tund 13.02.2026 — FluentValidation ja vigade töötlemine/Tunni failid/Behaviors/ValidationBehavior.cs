using FluentValidation;
using KooliProjekt.Application.Infrastructure;
using MediatR;

namespace KooliProjekt.Application.Behaviors;

/// <summary>Runs every FluentValidation validator before the request handler.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : OperationResult, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken)));
        var errors = validationResults.SelectMany(x => x.Errors).ToList();
        if (errors.Count == 0) return await next();

        var result = new TResponse();
        foreach (var error in errors) result.AddPropertyError(error.PropertyName, error.ErrorMessage);
        return result;
    }
}
