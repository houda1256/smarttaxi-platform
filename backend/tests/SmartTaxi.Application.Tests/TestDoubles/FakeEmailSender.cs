using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string Body)> SentMessages { get; } = [];

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        SentMessages.Add((toEmail, subject, body));
        return Task.CompletedTask;
    }
}
