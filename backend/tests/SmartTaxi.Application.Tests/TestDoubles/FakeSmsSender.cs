using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSmsSender : ISmsSender
{
    public List<(string ToPhoneNumber, string Message)> SentMessages { get; } = [];

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        SentMessages.Add((toPhoneNumber, message));
        return Task.CompletedTask;
    }
}
