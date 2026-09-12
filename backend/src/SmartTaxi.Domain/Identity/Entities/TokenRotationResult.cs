using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Identity.Entities;

public sealed record TokenRotationResult(TokenRotationOutcome Outcome, RefreshToken? NewToken)
{
    public static TokenRotationResult Success(RefreshToken newToken) => new(TokenRotationOutcome.Success, newToken);

    public static TokenRotationResult Failure(TokenRotationOutcome outcome) => new(outcome, null);
}
