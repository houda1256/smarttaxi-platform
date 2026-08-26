using SmartTaxi.Application.Advertising.Commands.CreateCampaign;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class CreateCampaignCommandHandlerTests
{
    private readonly FakeAdvertiserProfileRepository _profileRepository = new();
    private readonly FakeAdvertisingPlacementRepository _placementRepository = new();
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly CreateCampaignCommandHandler _handler;

    public CreateCampaignCommandHandlerTests()
    {
        _handler = new CreateCampaignCommandHandler(_profileRepository, _placementRepository, _campaignRepository);
    }

    private async Task<AdvertiserProfile> CreateProfileAsync()
    {
        var profile = AdvertiserProfile.Register(Guid.NewGuid(), "Acme", "Acme SARL", "TAX1", "Tunis", "addr", "a@acme.tn", null, DateTime.UtcNow);
        await _profileRepository.TryAddAsync(profile, CancellationToken.None);
        return profile;
    }

    private AdvertisingPlacement CreateActivePlacement()
    {
        var placement = AdvertisingPlacement.Create("SCREEN", "Screen", "desc", [AdMediaType.Image], DateTime.UtcNow);
        _placementRepository.Seed(placement);
        return placement;
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesDraftCampaign()
    {
        var profile = await CreateProfileAsync();
        var placement = CreateActivePlacement();

        var result = await _handler.Handle(
            new CreateCampaignCommand(
                profile.UserId, "Summer", "desc", "obj", placement.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
                AdPricingModel.Flat, 100m, 1000m, null, "Tunis", null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var campaign = await _campaignRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Draft, campaign!.Status);
    }

    [Fact]
    public async Task Handle_NoAdvertiserProfile_ReturnsNotFound()
    {
        var placement = CreateActivePlacement();

        var result = await _handler.Handle(
            new CreateCampaignCommand(
                Guid.NewGuid(), "Summer", "desc", "obj", placement.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
                AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_InactivePlacement_ReturnsValidationError()
    {
        var profile = await CreateProfileAsync();
        var placement = CreateActivePlacement();
        placement.Deactivate(DateTime.UtcNow);

        var result = await _handler.Handle(
            new CreateCampaignCommand(
                profile.UserId, "Summer", "desc", "obj", placement.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
                AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
