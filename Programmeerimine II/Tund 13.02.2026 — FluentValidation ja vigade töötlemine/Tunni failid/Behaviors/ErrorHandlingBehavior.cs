using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KooliProjekt.Application.Behaviors;

/// <summary>Converts an unexpected handler exception to an API-safe result.</summary>
public sealed class ErrorHandlingBehavior<TRequest, TResponse>(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : OperationResult, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception exception)
        {
            try
            {
                logger.LogError(exception, "Unhandled error while processing {RequestType}", typeof(TRequest).Name);
            }
            catch
            {
                // A broken logging provider must never prevent the client from receiving a safe error response.
            }
            var result = new TResponse();
            result.AddError("An unexpected server error occurred.");
            return result;
        }
    }
}
