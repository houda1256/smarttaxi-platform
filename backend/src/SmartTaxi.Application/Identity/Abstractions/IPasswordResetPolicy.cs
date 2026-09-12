namespace SmartTaxi.Application.Identity.Abstractions;

public interface IPasswordResetPolicy
{
    TimeSpan TokenLifetime { get; }
}
