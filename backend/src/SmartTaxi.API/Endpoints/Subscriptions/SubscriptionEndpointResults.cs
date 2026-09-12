using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Common;

namespace SmartTaxi.API.Endpoints.Subscriptions;

/// <summary>Same simplified Result-to-ProblemDetails mapping used by every other module's endpoints — see Payments/PaymentEndpointResults for the rationale.</summary>
internal static class SubscriptionEndpointResults
{
    public static ProblemHttpResult ToProblem(this Result result) =>
        TypedResults.Problem(detail: result.Error, statusCode: MapStatusCode(result.ErrorType));

    public static ProblemHttpResult ToProblem<T>(this Result<T> result) =>
        TypedResults.Problem(detail: result.Error, statusCode: MapStatusCode(result.ErrorType));

    private static int MapStatusCode(ErrorType? errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
