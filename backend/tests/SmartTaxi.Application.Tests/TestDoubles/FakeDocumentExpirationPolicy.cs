using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDocumentExpirationPolicy : IDocumentExpirationPolicy
{
    public int ReminderLeadDays { get; init; } = 30;
}
