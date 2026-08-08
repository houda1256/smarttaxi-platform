namespace SmartTaxi.Domain.Identity.Enums;

public enum TokenRotationOutcome
{
    Success,
    TokenNotFound,
    TokenExpired,
    SessionExpired,
    SessionRevoked,
    ReuseDetected
}
