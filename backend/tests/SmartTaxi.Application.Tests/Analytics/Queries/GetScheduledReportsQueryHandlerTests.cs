using SmartTaxi.Application.Analytics.Queries.GetScheduledReports;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Tests.Analytics.Queries;

public class GetScheduledReportsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllDefinitions()
    {
        var repository = new FakeScheduledReportRepository();
        await repository.TryAddAsync(
            ScheduledReportDefinition.Create(
                ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow),
            CancellationToken.None);
        await repository.TryAddAsync(
            ScheduledReportDefinition.Create(
                ScheduledReportCategory.Support, ScheduledReportFrequency.Weekly, Guid.NewGuid(), DateTime.UtcNow.AddDays(7),
                DateTime.UtcNow),
            CancellationToken.None);

        var handler = new GetScheduledReportsQueryHandler(repository);
        var result = await handler.Handle(new GetScheduledReportsQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPage()
    {
        var handler = new GetScheduledReportsQueryHandler(new FakeScheduledReportRepository());

        var result = await handler.Handle(new GetScheduledReportsQuery(1, 10), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
