using SmartTaxi.Application.Advertising;
using SmartTaxi.Application.Advertising.Commands.UploadCampaignMedia;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class UploadCampaignMediaCommandHandlerTests
{
    private readonly FakeAdCampaignRepository _campaignRepository = new();
    private readonly FakeAdvertisingPlacementRepository _placementRepository = new();
    private readonly FakeCampaignCreativeRepository _creativeRepository = new();
    private readonly FakeAdCampaignReviewHistoryRepository _reviewHistoryRepository = new();
    private readonly FakeFileSecurityScanner _scanner = new();
    private readonly FakeFileStorageService _fileStorage = new();
    private readonly FakeAdvertisingMediaStorageCleaner _storageCleaner;
    private readonly UploadCampaignMediaCommandHandler _handler;
    private readonly AdvertisingPlacement _placement;

    public UploadCampaignMediaCommandHandlerTests()
    {
        var validator = new CampaignMediaUploadValidator(new FakeAdvertisingMediaUploadPolicy());
        _storageCleaner = new FakeAdvertisingMediaStorageCleaner(_fileStorage);
        _handler = new UploadCampaignMediaCommandHandler(
            _campaignRepository, _placementRepository, _creativeRepository, _reviewHistoryRepository, validator, _scanner, _fileStorage,
            _storageCleaner);
        _placement = AdvertisingPlacement.Create("SCREEN", "Screen", "desc", [AdMediaType.Image], DateTime.UtcNow);
        _placementRepository.Seed(_placement);
    }

    private async Task<AdCampaign> CreateDraftCampaignAsync()
    {
        var campaign = AdCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Name", "desc", "obj", _placement.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(10),
            AdPricingModel.Flat, 100m, 1000m, null, null, null, null, null, null, DateTime.UtcNow);
        await _campaignRepository.AddAsync(campaign, CancellationToken.None);
        return campaign;
    }

    [Fact]
    public async Task Handle_SafeContent_UploadsCreative()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = true;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _fileStorage.SavedFileCount);
    }

    [Fact]
    public async Task Handle_FailedScan_BlocksUploadAndSavesNothing()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = false;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(0, _fileStorage.SavedFileCount);
        Assert.Empty(await _creativeRepository.GetForCampaignAsync(campaign.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnsupportedMimeType_ReturnsValidationError()
    {
        var campaign = await CreateDraftCampaignAsync();
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "application/x-msdownload", "a.exe", content),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ReplaceMediaOnApprovedCampaign_ForcesResubmission()
    {
        var campaign = await CreateDraftCampaignAsync();
        await _campaignRepository.TryTransitionAsync(campaign.Id, [AdCampaignStatus.Draft], AdCampaignStatus.Approved, true, Guid.NewGuid(), null, DateTime.UtcNow, CancellationToken.None);

        await using var content = new MemoryStream([1, 2, 3, 4]);
        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _campaignRepository.GetByIdAsync(campaign.Id, CancellationToken.None);
        Assert.Equal(AdCampaignStatus.Submitted, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_SuccessfulUpload_NeverTriggersCleanup()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = true;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_storageCleaner.AttemptedDeletes);
        Assert.Equal(1, _fileStorage.SavedFileCount);
    }

    [Fact]
    public async Task Handle_DbPersistenceFailsAfterStorageWrite_DeletesOrphanedFileAndPropagatesOriginalException()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = true;
        _creativeRepository.ThrowOnPersist = true;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None));

        Assert.Contains("Simulated DB persistence failure", exception.Message);
        Assert.Single(_storageCleaner.AttemptedDeletes);
        Assert.Equal(0, _fileStorage.SavedFileCount);
    }

    [Fact]
    public async Task Handle_CleanupItselfFails_OriginalDbExceptionStillPropagates()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = true;
        _creativeRepository.ThrowOnPersist = true;
        _fileStorage.ThrowOnDelete = true;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None));

        Assert.Contains("Simulated DB persistence failure", exception.Message);
    }

    [Fact]
    public async Task Handle_DbPersistenceFailsDuringReplacement_NeverDeletesThePreviousValidCreative()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = true;
        await using (var firstContent = new MemoryStream([1, 2, 3, 4]))
        {
            var firstResult = await _handler.Handle(
                new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", firstContent),
                CancellationToken.None);
            Assert.True(firstResult.IsSuccess);
        }

        var previousStorageKeys = new HashSet<string>(_storageCleaner.AttemptedDeletes);
        Assert.Empty(previousStorageKeys);
        var previousCreative = (await _creativeRepository.GetForCampaignAsync(campaign.Id, CancellationToken.None)).Single();

        _creativeRepository.ThrowOnPersist = true;
        await using var secondContent = new MemoryStream([5, 6, 7, 8]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "b.png", secondContent),
            CancellationToken.None));

        Assert.DoesNotContain(previousCreative.StorageKey, _storageCleaner.AttemptedDeletes);
        Assert.True(_fileStorage.Contains(previousCreative.StorageKey));
    }

    [Fact]
    public async Task Handle_FailedSecurityScan_PerformsZeroStorageWrites()
    {
        var campaign = await CreateDraftCampaignAsync();
        _scanner.NextScanIsSafe = false;
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var result = await _handler.Handle(
            new UploadCampaignMediaCommand(campaign.Id, campaign.AdvertiserUserId, AdMediaType.Image, "image/png", "a.png", content),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _fileStorage.SavedFileCount);
        Assert.Empty(_storageCleaner.AttemptedDeletes);
    }
}
