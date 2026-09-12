namespace SmartTaxi.Application.Identity.Abstractions;

public interface IEmailVerificationPolicy
{
    TimeSpan TokenLifetime { get; }

    TimeSpan ResendInterval { get; }
}
