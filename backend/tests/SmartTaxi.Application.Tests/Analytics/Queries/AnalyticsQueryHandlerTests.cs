using SmartTaxi.Application.Analytics.Queries.GetAdminDashboard;
using SmartTaxi.Application.Analytics.Queries.GetAdvertisingAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFinancialAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFleetAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetGrowthAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetMaintenanceAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRoadsideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetSubscriptionAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetSupportAnalytics;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Analytics.Queries;

public class AnalyticsQueryHandlerTests
{
    private static readonly DateTime FromUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc = FromUtc.AddDays(30);
    private static readonly DateTime InvalidToUtc = FromUtc.AddDays(-1);

    [Fact]
    public async Task GetAdminDashboard_DelegatesToReader()
    {
        var reader = new FakeAdminDashboardReader { Summary = new(1, 2, 3, 4, 5, 6, 7, 8m, 9m, 10, 11, 12, 13, 14, 15) };
        var handler = new GetAdminDashboardQueryHandler(reader);

        var result = await handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        Assert.Equal(1, reader.CallCount);
        Assert.Equal(1, result.TotalUsers);
        Assert.Equal(15, result.ActiveCampaigns);
    }

    [Fact]
    public async Task GetGrowthAnalytics_InvalidRange_ReturnsValidation()
    {
        var handler = new GetGrowthAnalyticsQueryHandler(new FakeGrowthAnalyticsReader());

        var result = await handler.Handle(new GetGrowthAnalyticsQuery(FromUtc, InvalidToUtc), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetGrowthAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeGrowthAnalyticsReader();
        var handler = new GetGrowthAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetGrowthAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetRideAnalytics_InvalidRange_ReturnsValidation()
    {
        var handler = new GetRideAnalyticsQueryHandler(new FakeRideAnalyticsReader());

        var result = await handler.Handle(new GetRideAnalyticsQuery(FromUtc, InvalidToUtc), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetRideAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeRideAnalyticsReader();
        var handler = new GetRideAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetRideAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetFleetAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeFleetAnalyticsReader();
        var handler = new GetFleetAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetFleetAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetSubscriptionAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeSubscriptionAnalyticsReader();
        var handler = new GetSubscriptionAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetSubscriptionAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetAdvertisingAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeAdvertisingAnalyticsReader();
        var handler = new GetAdvertisingAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetAdvertisingAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetMaintenanceAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeMaintenanceAnalyticsReader();
        var handler = new GetMaintenanceAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetMaintenanceAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetRoadsideAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeRoadsideAnalyticsReader();
        var handler = new GetRoadsideAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetRoadsideAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetSupportAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeSupportAnalyticsReader();
        var handler = new GetSupportAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetSupportAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetFinancialAnalytics_ValidRange_DelegatesToReader()
    {
        var reader = new FakeFinancialAnalyticsReader();
        var handler = new GetFinancialAnalyticsQueryHandler(reader);

        var result = await handler.Handle(new GetFinancialAnalyticsQuery(FromUtc, ToUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((FromUtc, ToUtc), reader.LastRequest);
    }

    [Fact]
    public async Task GetFinancialAnalytics_InvalidRange_ReturnsValidation()
    {
        var handler = new GetFinancialAnalyticsQueryHandler(new FakeFinancialAnalyticsReader());

        var result = await handler.Handle(new GetFinancialAnalyticsQuery(FromUtc, InvalidToUtc), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
