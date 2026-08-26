using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Advertising.Events;

namespace SmartTaxi.Domain.Tests.Advertising.Entities;

public class AdCampaignTests
{
    private static AdCampaign CreateValid(DateTime? start = null, DateTime? end = null, decimal budget = 1000m, decimal? dailyBudget = null) =>
        AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Summer Promo", "desc", "objective", Guid.NewGuid(), start ?? DateTime.UtcNow.AddDays(1),
            end ?? DateTime.UtcNow.AddDays(10), AdPricingModel.Cpm, 5m, budget, dailyBudget, "Tunis", "Standard", "Mon,Tue", 8, 20,
            DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_StartsInDraftAndRaisesEvent()
    {
        var campaign = CreateValid();

        Assert.Equal(AdCampaignStatus.Draft, campaign.Status);
        Assert.Equal(0m, campaign.ConsumedBudget);
        Assert.StartsWith("CAMP-", campaign.Code);
        Assert.IsType<AdCampaignCreated>(Assert.Single(campaign.DomainEvents));
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(start: DateTime.UtcNow.AddDays(5), end: DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_NegativeBudget_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(budget: -1m));
    }

    [Fact]
    public void Create_DailyBudgetExceedingTotalBudget_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(budget: 100m, dailyBudget: 200m));
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AdCampaign.Create(
                Guid.NewGuid(), Guid.NewGuid(), "", "d", "o", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2),
                AdPricingModel.Flat, 10m, 100m, null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_InvalidTargetHour_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AdCampaign.Create(
                Guid.NewGuid(), Guid.NewGuid(), "n", "d", "o", Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2),
                AdPricingModel.Flat, 10m, 100m, null, null, null, null, 25, null, DateTime.UtcNow));
    }

    [Fact]
    public void RemainingBudget_ReflectsConsumedBudget()
    {
        var campaign = CreateValid(budget: 1000m);

        Assert.Equal(1000m, campaign.RemainingBudget);
    }

    [Fact]
    public void WouldBeMaterialChange_SameValues_ReturnsFalse()
    {
        var campaign = CreateValid();

        var material = campaign.WouldBeMaterialChange(
            campaign.PlacementId, campaign.StartAtUtc, campaign.EndAtUtc, campaign.PricingModel, campaign.PriceRate, campaign.BudgetLimit,
            campaign.DailyBudgetLimit, campaign.TargetCity, campaign.TargetVehicleCategory, campaign.TargetDaysOfWeek,
            campaign.TargetStartHour, campaign.TargetEndHour);

        Assert.False(material);
    }

    [Theory]
    [InlineData("budget")]
    [InlineData("dates")]
    [InlineData("pricing")]
    [InlineData("targeting")]
    public void WouldBeMaterialChange_ChangedField_ReturnsTrue(string changedAspect)
    {
        var campaign = CreateValid();
        var placementId = campaign.PlacementId;
        var start = campaign.StartAtUtc;
        var end = campaign.EndAtUtc;
        var pricingModel = campaign.PricingModel;
        var priceRate = campaign.PriceRate;
        var budget = campaign.BudgetLimit;
        var dailyBudget = campaign.DailyBudgetLimit;
        var city = campaign.TargetCity;

        switch (changedAspect)
        {
            case "budget": budget += 100m; break;
            case "dates": end = end.AddDays(1); break;
            case "pricing": priceRate += 1m; break;
            case "targeting": city = "Sfax"; break;
        }

        var material = campaign.WouldBeMaterialChange(
            placementId, start, end, pricingModel, priceRate, budget, dailyBudget, city, campaign.TargetVehicleCategory,
            campaign.TargetDaysOfWeek, campaign.TargetStartHour, campaign.TargetEndHour);

        Assert.True(material);
    }

    [Fact]
    public void UpdateFields_CosmeticOnlyChange_IsNotConsideredWhenComparedByWouldBeMaterialChange()
    {
        var campaign = CreateValid();

        var material = campaign.WouldBeMaterialChange(
            campaign.PlacementId, campaign.StartAtUtc, campaign.EndAtUtc, campaign.PricingModel, campaign.PriceRate, campaign.BudgetLimit,
            campaign.DailyBudgetLimit, campaign.TargetCity, campaign.TargetVehicleCategory, campaign.TargetDaysOfWeek,
            campaign.TargetStartHour, campaign.TargetEndHour);

        campaign.UpdateFields(
            "New Name", "New Description", campaign.Objective, campaign.PlacementId, campaign.StartAtUtc, campaign.EndAtUtc,
            campaign.PricingModel, campaign.PriceRate, campaign.BudgetLimit, campaign.DailyBudgetLimit, campaign.TargetCity,
            campaign.TargetVehicleCategory, campaign.TargetDaysOfWeek, campaign.TargetStartHour, campaign.TargetEndHour, DateTime.UtcNow);

        Assert.False(material);
        Assert.Equal("New Name", campaign.Name);
    }
}
