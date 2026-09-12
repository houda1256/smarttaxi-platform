using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RecordClick;

/// <summary>
/// Requires a valid, server-issued AdDeliveryToken (Module 8 audit fix #1) —
/// a click can no longer be reported without proof of a legitimate prior
/// delivery context, closing the same fabricated-idempotency-key gap as
/// impressions. The IdempotencyKey used for the fact row is derived from the
/// token's own DeliveryId, so replaying the same token never double-charges.
/// No passenger/user identity is ever stored. Only Cpc campaigns charge per
/// click; other pricing models record the click as a zero-cost fact for CTR
/// purposes.
/// </summary>
public sealed class RecordClickCommandHandler : ICommandHandler<RecordClickCommand, Result<Guid>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string PlacementMismatchError = "Cet emplacement ne correspond pas à la campagne.";
    private const string ImpressionMismatchError = "Cette impression ne correspond pas à la campagne.";
    private const string InvalidTokenError = "Jeton de diffusion invalide.";
    private const string InvalidTimestampError = "L'horodatage fourni est invalide.";
    private const string NotActiveError = "Seule une campagne active peut recevoir des clics.";
    private const string BudgetExceededError = "Le budget de la campagne est épuisé.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingImpressionRepository _impressionRepository;
    private readonly IAdvertisingClickRepository _clickRepository;
    private readonly IAdDeliveryTokenService _tokenService;

    public RecordClickCommandHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingImpressionRepository impressionRepository,
        IAdvertisingClickRepository clickRepository, IAdDeliveryTokenService tokenService)
    {
        _campaignRepository = campaignRepository;
        _impressionRepository = impressionRepository;
        _clickRepository = clickRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<Guid>> Handle(RecordClickCommand command, CancellationToken cancellationToken)
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

        if (command.ImpressionId is not null)
        {
            var impression = await _impressionRepository.GetByIdAsync(command.ImpressionId.Value, cancellationToken);

            if (impression is null || impression.CampaignId != campaign.Id)
            {
                return Result<Guid>.Failure(ImpressionMismatchError, ErrorType.Validation);
            }
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

        var operationalCost = campaign.PricingModel == AdPricingModel.Cpc ? campaign.PriceRate : 0m;
        var idempotencyKey = $"click:{validation.DeliveryId:N}";

        var result = await _clickRepository.TryRecordAsync(
            campaign.Id, campaign.PlacementId, command.ImpressionId, idempotencyKey, operationalCost, command.OccurredAtUtc, utcNow,
            cancellationToken);

        return result.Outcome switch
        {
            AdvertisingFactOutcome.CampaignNotActive => Result<Guid>.Failure(NotActiveError, ErrorType.Conflict),
            AdvertisingFactOutcome.BudgetExceeded => Result<Guid>.Failure(BudgetExceededError, ErrorType.Conflict),
            _ => Result<Guid>.Success(result.FactId!.Value)
        };
    }
}
