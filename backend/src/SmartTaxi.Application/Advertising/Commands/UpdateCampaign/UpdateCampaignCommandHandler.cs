using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using ReviewAction = SmartTaxi.Domain.Advertising.Enums.AdCampaignReviewAction;

namespace SmartTaxi.Application.Advertising.Commands.UpdateCampaign;

/// <summary>
/// Draft/ChangesRequested: free edit, no status change (only the owning
/// advertiser can be editing a pre-review campaign, so there is no reviewer
/// race to protect against). Approved/Scheduled/Active/Paused with a MATERIAL
/// change (placement/dates/pricing/budget/targeting — see
/// AdCampaign.WouldBeMaterialChange): the status is atomically transitioned
/// back to Submitted FIRST, before any field is touched — this closes the
/// race with a concurrent admin review (Module 8 audit's mandatory
/// "edit vs review" concurrency scenario): if a concurrent ApproveCampaign
/// call is racing against this same status, only one of them can win the
/// conditional UPDATE, and the loser fails cleanly rather than silently
/// preserving a stale Approved status alongside newly-edited content. A
/// cosmetic-only change (Name/Description/Objective) never touches Status.
/// Submitted/UnderReview/Suspended/Completed/Cancelled/Rejected are never
/// editable.
/// </summary>
public sealed class UpdateCampaignCommandHandler : ICommandHandler<UpdateCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez modifier que vos propres campagnes.";
    private const string NotEditableError = "Cette campagne ne peut pas être modifiée dans son état actuel.";
    private const string ConcurrentReviewError = "Cette campagne a été modifiée par une autre opération — veuillez réessayer.";
    private const string PlacementNotFoundError = "Emplacement publicitaire introuvable.";
    private const string PlacementInactiveError = "Cet emplacement publicitaire n'est plus disponible.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> FreelyEditableStatuses =
        [AdCampaignStatus.Draft, AdCampaignStatus.ChangesRequested];

    private static readonly IReadOnlyCollection<AdCampaignStatus> MaterialEditRequiresReReviewStatuses =
        [AdCampaignStatus.Approved, AdCampaignStatus.Scheduled, AdCampaignStatus.Active, AdCampaignStatus.Paused];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingPlacementRepository _placementRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public UpdateCampaignCommandHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingPlacementRepository placementRepository,
        IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _placementRepository = placementRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<Result> Handle(UpdateCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var isFreelyEditable = FreelyEditableStatuses.Contains(campaign.Status);
        var isMaterialEditStatus = MaterialEditRequiresReReviewStatuses.Contains(campaign.Status);

        if (!isFreelyEditable && !isMaterialEditStatus)
        {
            return Result.Failure(NotEditableError, ErrorType.Conflict);
        }

        var placement = await _placementRepository.GetByIdAsync(command.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result.Failure(PlacementNotFoundError, ErrorType.NotFound);
        }

        if (!placement.IsActive)
        {
            return Result.Failure(PlacementInactiveError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var originalStatus = campaign.Status;

        var isMaterial = isMaterialEditStatus && campaign.WouldBeMaterialChange(
            command.PlacementId, command.StartAtUtc, command.EndAtUtc, command.PricingModel, command.PriceRate, command.BudgetLimit,
            command.DailyBudgetLimit, command.TargetCity, command.TargetVehicleCategory, command.TargetDaysOfWeek,
            command.TargetStartHour, command.TargetEndHour);

        if (isMaterial)
        {
            // Claim the campaign for editing atomically BEFORE touching any field — closes the race
            // with a concurrent admin review of the still-Approved/Scheduled/Active/Paused campaign.
            var claimed = await _campaignRepository.TryTransitionAsync(
                campaign.Id, [originalStatus], AdCampaignStatus.Submitted, touchReviewMetadata: true, reviewedByUserId: null,
                reviewReason: null, utcNow, cancellationToken);

            if (!claimed)
            {
                return Result.Failure(ConcurrentReviewError, ErrorType.Conflict);
            }
        }

        try
        {
            campaign.UpdateFields(
                command.Name, command.Description, command.Objective, command.PlacementId, command.StartAtUtc, command.EndAtUtc,
                command.PricingModel, command.PriceRate, command.BudgetLimit, command.DailyBudgetLimit, command.TargetCity,
                command.TargetVehicleCategory, command.TargetDaysOfWeek, command.TargetStartHour, command.TargetEndHour, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _campaignRepository.UpdateAsync(campaign, cancellationToken);

        if (isMaterial)
        {
            await _reviewHistoryRepository.AddAsync(
                AdCampaignReviewHistoryEntry.Record(
                    campaign.Id, command.RequestingUserId, ReviewAction.ResubmittedAfterMaterialChange,
                    originalStatus, AdCampaignStatus.Submitted, "Modification substantielle après approbation", utcNow),
                cancellationToken);
        }

        return Result.Success();
    }
}
