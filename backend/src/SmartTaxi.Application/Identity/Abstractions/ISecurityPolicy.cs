namespace SmartTaxi.Application.Identity.Abstractions;

public interface ISecurityPolicy
{
    bool RevokeOtherSessionsOnPasswordChange { get; }
}
