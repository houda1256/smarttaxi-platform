using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Common;

namespace SmartTaxi.API.Endpoints.Rides;

/// <summary>
/// A single Result-to-ProblemDetails mapping shared by every Ride endpoint,
/// instead of Fleet's per-endpoint typed-union switch — a deliberate
/// simplification given the Ride module's exceptionally large endpoint
/// surface (~40 endpoints); documented in the 4o final report.
/// </summary>
internal static class RideEndpointResults
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
