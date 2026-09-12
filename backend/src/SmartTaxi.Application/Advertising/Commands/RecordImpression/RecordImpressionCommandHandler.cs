using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RecordImpression;

/// <summary>
/// Requires a valid, server-issued AdDeliveryToken (Module 8 audit fix #1) —
/// a bare client-supplied idempotency key is no longer accepted. The
/// IdempotencyKey used for the actual fact row is derived deterministically
/// from the token's own DeliveryId, never chosen by the caller, so a replay
/// of the exact same token can never consume budget twice, and a client can
/// never fabricate a fresh "unique" key to bypass this. No passenger/user
/// identity is ever stored — only campaign/placement context. Cost is
/// computed server-side from the campaign's own PricingModel/PriceRate,
/// never trusted from the caller. Cpm is the only model that charges
/// per-impression (rate per 1000 impressions); Flat/Duration/Cpc impressions
/// carry zero operational cost (Cpc charges on the click instead).
/// </summary>
public sealed class RecordImpressionCommandHandler : ICommandHandler<RecordImpressionCommand, Result<Guid>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string PlacementMismatchError = "Cet emplacement ne correspond pas à la campagne.";
    private const string InvalidTokenError = "Jeton de diffusion invalide.";
    private const string InvalidTimestampError = "L'horodatage fourni est invalide.";
    private const string NotActiveError = "Seule une campagne active peut recevoir des impressions.";
    private const string BudgetExceededError = "Le budget de la campagne est épuisé.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingImpressionRepository _impressionRepository;
    private readonly IAdDeliveryTokenService _tokenService;

    public RecordImpressionCommandHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingImpressionRepository impressionRepository, IAdDeliveryTokenService tokenService)
    {
        _campaignRepository = campaignRepository;
        _impressionRepository = impressionRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<Guid>> Handle(RecordImpressionCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.PlacementId != command.PlacementId)
        {
            return Result<Guid>.Failure(PlacementMismatchError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;

        var validation = _tokenService.Validate(command.DeliveryToken, command.CampaignId, command.PlacementId, command.RequestingUserId, utcNow);

        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(validation.FailureReason ?? InvalidTokenError, ErrorType.Forbidden);
        }

        if (!AdvertisingFactTimestampValidator.IsReasonable(command.OccurredAtUtc, utcNow))
        {
            return Result<Guid>.Failure(InvalidTimestampError, ErrorType.Validation);
        }

        var operationalCost = campaign.PricingModel == AdPricingModel.Cpm ? campaign.PriceRate / 1000m : 0m;
        var idempotencyKey = $"impression:{validation.DeliveryId:N}";

        var result = await _impressionRepository.TryRecordAsync(
            campaign.Id, campaign.PlacementId, idempotencyKey, operationalCost, command.OccurredAtUtc, utcNow, cancellationToken);

        return result.Outcome switch
        {
            AdvertisingFactOutcome.CampaignNotActive => Result<Guid>.Failure(NotActiveError, ErrorType.Conflict),
            AdvertisingFactOutcome.BudgetExceeded => Result<Guid>.Failure(BudgetExceededError, ErrorType.Conflict),
            _ => Result<Guid>.Success(result.FactId!.Value)
        };
    }
}
