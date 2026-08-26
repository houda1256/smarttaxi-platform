using SmartTaxi.Application.RoadsideAssistance.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRoadsideExpiryPolicy : IRoadsideExpiryPolicy
{
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromHours(2);
}
