using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.UploadCampaignMedia;

/// <summary>
/// Handles both first upload (no existing creative) and replacement (new
/// version, previous row atomically marked Replaced — never edited in
/// place). A failed IFileSecurityScanner result blocks the upload entirely —
/// nothing is saved to storage and no creative row is created. Replacing
/// media on an Approved/Scheduled/Active/Paused campaign is always a material
/// change: the campaign is atomically claimed back to Submitted BEFORE the
/// new creative is persisted, the same race-safe pattern as
/// UpdateCampaignCommandHandler.
/// </summary>
public sealed class UploadCampaignMediaCommandHandler : ICommandHandler<UploadCampaignMediaCommand, Result<Guid>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez ajouter des médias qu'à vos propres campagnes.";
    private const string NotEditableError = "Un média ne peut pas être ajouté à cette campagne dans son état actuel.";
    private const string PlacementNotFoundError = "Emplacement publicitaire introuvable.";
    private const string UnsupportedMediaTypeError = "Ce type de média n'est pas supporté par l'emplacement choisi.";
    private const string ScanFailedError = "Le fichier n'a pas passé le contrôle de sécurité.";
    private const string ConcurrentReviewError = "Cette campagne a été modifiée par une autre opération — veuillez réessayer.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> FreelyEditableStatuses = [AdCampaignStatus.Draft, AdCampaignStatus.ChangesRequested];

    private static readonly IReadOnlyCollection<AdCampaignStatus> MaterialEditRequiresReReviewStatuses =
        [AdCampaignStatus.Approved, AdCampaignStatus.Scheduled, AdCampaignStatus.Active, AdCampaignStatus.Paused];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingPlacementRepository _placementRepository;
    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly CampaignMediaUploadValidator _validator;
    private readonly IFileSecurityScanner _scanner;
    private readonly IFileStorageService _fileStorage;
    private readonly IAdvertisingMediaStorageCleaner _storageCleaner;

    public UploadCampaignMediaCommandHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingPlacementRepository placementRepository,
        ICampaignCreativeRepository creativeRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        CampaignMediaUploadValidator validator, IFileSecurityScanner scanner, IFileStorageService fileStorage,
        IAdvertisingMediaStorageCleaner storageCleaner)
    {
        _campaignRepository = campaignRepository;
        _placementRepository = placementRepository;
        _creativeRepository = creativeRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _validator = validator;
        _scanner = scanner;
        _fileStorage = fileStorage;
        _storageCleaner = storageCleaner;
    }

    public async Task<Result<Guid>> Handle(UploadCampaignMediaCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var isFreelyEditable = FreelyEditableStatuses.Contains(campaign.Status);
        var isMaterialEditStatus = MaterialEditRequiresReReviewStatuses.Contains(campaign.Status);

        if (!isFreelyEditable && !isMaterialEditStatus)
        {
            return Result<Guid>.Failure(NotEditableError, ErrorType.Conflict);
        }

        var placement = await _placementRepository.GetByIdAsync(campaign.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result<Guid>.Failure(PlacementNotFoundError, ErrorType.NotFound);
        }

        if (!placement.Supports(command.MediaType))
        {
            return Result<Guid>.Failure(UnsupportedMediaTypeError, ErrorType.Validation);
        }

        var validation = await _validator.ValidateAndReadAsync(command.Content, command.DeclaredMimeType, cancellationToken);

        if (!validation.IsSuccess)
        {
            return Result<Guid>.Failure(validation.Error!, validation.ErrorType!.Value);
        }

        var content = validation.Value!;
        var scanResult = await _scanner.ScanAsync(content.Bytes, command.DeclaredMimeType, cancellationToken);

        if (!scanResult.IsSafe)
        {
            return Result<Guid>.Failure(scanResult.RejectionReason ?? ScanFailedError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;

        if (isMaterialEditStatus)
        {
            var claimed = await _campaignRepository.TryTransitionAsync(
                campaign.Id, [campaign.Status], AdCampaignStatus.Submitted, touchReviewMetadata: true, reviewedByUserId: null,
                reviewReason: null, utcNow, cancellationToken);

            if (!claimed)
            {
                return Result<Guid>.Failure(ConcurrentReviewError, ErrorType.Conflict);
            }
        }

        var storageKey = CampaignMediaFileNaming.CreateStorageKey(campaign.Id, command.DeclaredMimeType);
        await using (var writeStream = new MemoryStream(content.Bytes))
        {
            await _fileStorage.SaveAsync(storageKey, writeStream, cancellationToken);
        }

        var displayName = CampaignMediaFileNaming.SanitizeDisplayFileName(command.OriginalFileName);
        var current = await _creativeRepository.GetCurrentForCampaignAsync(campaign.Id, cancellationToken);

        CampaignCreative creative;

        try
        {
            if (current is null)
            {
                creative = CampaignCreative.Upload(campaign.Id, command.MediaType, command.DeclaredMimeType, displayName, storageKey, content.Bytes.LongLength, content.Sha256, utcNow);
                await _creativeRepository.AddAsync(creative, cancellationToken);
            }
            else
            {
                creative = current.CreateReplacement(command.MediaType, command.DeclaredMimeType, displayName, storageKey, content.Bytes.LongLength, content.Sha256, utcNow);
                await _creativeRepository.ReplaceAsync(current.Id, creative, utcNow, cancellationToken);
            }
        }
        catch
        {
            // The object we just wrote (storageKey, freshly generated in this call) is now orphaned — best-
            // effort delete it. This never touches `current`'s own (older, still valid) storage key. Cleanup
            // itself never throws, so the ORIGINAL exception is always what propagates from this catch.
            await _storageCleaner.DeleteBestEffortAsync(storageKey, cancellationToken);
            throw;
        }

        if (isMaterialEditStatus)
        {
            await _reviewHistoryRepository.AddAsync(
                AdCampaignReviewHistoryEntry.Record(
                    campaign.Id, command.RequestingUserId, AdCampaignReviewAction.ResubmittedAfterMaterialChange, campaign.Status,
                    AdCampaignStatus.Submitted, "Remplacement de média après approbation", utcNow),
                cancellationToken);
        }

        return Result<Guid>.Success(creative.Id);
    }
}
