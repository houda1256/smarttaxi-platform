using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RequestAdDelivery;

/// <summary>
/// The smallest safe "issue a delivery context" flow — deliberately not an ad
/// SELECTION/serving engine (no targeting evaluation, no choosing among
/// campaigns): the caller already knows which campaign/placement it wants to
/// report on (from wherever it decided to display that ad), and this only
/// proves that choice was legitimate (campaign Active, placement matches,
/// schedule not actually over) at the moment of issuance, then mints a token
/// bound to exactly those values plus the caller's own identity.
/// </summary>
public sealed class RequestAdDeliveryCommandHandler : ICommandHandler<RequestAdDeliveryCommand, Result<string>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotActiveError = "Seule une campagne active peut être diffusée.";
    private const string PlacementMismatchError = "Cet emplacement ne correspond pas à la campagne.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdDeliveryTokenService _tokenService;

    public RequestAdDeliveryCommandHandler(IAdCampaignRepository campaignRepository, IAdDeliveryTokenService tokenService)
    {
        _campaignRepository = campaignRepository;
        _tokenService = tokenService;
    }

    public async Task<Result<string>> Handle(RequestAdDeliveryCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<string>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        // Defense-in-depth (Module 8 audit LOW finding): Status can lag briefly behind EndAtUtc until the
        // next ProcessCompletedCampaigns sweep — reject at issuance even if still nominally Active.
        if (campaign.Status != AdCampaignStatus.Active || utcNow >= campaign.EndAtUtc)
        {
            return Result<string>.Failure(NotActiveError, ErrorType.Conflict);
        }

        if (campaign.PlacementId != command.PlacementId)
        {
            return Result<string>.Failure(PlacementMismatchError, ErrorType.Validation);
        }

        var token = _tokenService.IssueToken(campaign.Id, campaign.PlacementId, command.RequestingUserId, utcNow);
        return Result<string>.Success(token);
    }
}
