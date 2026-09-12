using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

// Development/mock implementation only — no real SMS provider is integrated.
// Deliberately logs only non-sensitive metadata: never the message content
// (which carries the raw OTP) and never the full phone number.
internal sealed class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SMS queued for {MaskedRecipient}.", Mask(toPhoneNumber));
        return Task.CompletedTask;
    }

    private static string Mask(string phoneNumber) =>
        phoneNumber.Length <= 4 ? "***" : $"***{phoneNumber[^4..]}";
}
