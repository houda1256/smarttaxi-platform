namespace SmartTaxi.Domain.Identity.Enums;

public enum SessionRevocationReason
{
    LoggedOut,
    ManualRevocation,
    ReuseDetected,
    PasswordReset,
    PasswordChanged
}
