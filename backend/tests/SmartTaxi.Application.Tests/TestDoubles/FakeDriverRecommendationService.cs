using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverRecommendationService : IDriverRecommendationService
{
    public IReadOnlyCollection<DriverRecommendationResult> Results { get; set; } = [];

    public Task<IReadOnlyCollection<DriverRecommendationResult>> GetRecommendationsAsync(
        DriverRecommendationCriteria criteria, CancellationToken cancellationToken) =>
        Task.FromResult(Results);
}
