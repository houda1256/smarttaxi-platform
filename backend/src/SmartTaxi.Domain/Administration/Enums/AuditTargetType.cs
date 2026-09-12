namespace SmartTaxi.Domain.Administration.Enums;

/// <summary>Only User exists today — every approved event in this pass targets a User. Extending this for a future module's audit events is a plain enum addition, not a schema change.</summary>
public enum AuditTargetType
{
    User
}
