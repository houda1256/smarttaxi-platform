using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

// Development/mock implementation only — no real email provider is integrated.
// Deliberately logs only non-sensitive metadata: never the body (which carries
// the raw token/OTP) and never the full recipient address.
internal sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email queued for {MaskedRecipient} with subject '{Subject}'.", Mask(toEmail), subject);
        return Task.CompletedTask;
    }

    private static string Mask(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[atIndex..]}";
    }
}
