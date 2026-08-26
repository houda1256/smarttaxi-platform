using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Commands.CreateCampaign;

/// <summary>
/// Requires an existing AdvertiserProfile (Identity/professional-account
/// approval already happened before a profile can even be registered — see
/// RegisterAdvertiserProfileCommandHandler) and an active Placement. Never
/// trusts a client-supplied advertiser identity — AdvertiserUserId is always
/// the caller's own JWT subject, resolved by the endpoint before this command
/// is built.
/// </summary>
public sealed class CreateCampaignCommandHandler : ICommandHandler<CreateCampaignCommand, Result<Guid>>
{
    private const string NoProfileError = "Aucun profil annonceur trouvé — veuillez d'abord créer votre profil.";
    private const string PlacementNotFoundError = "Emplacement publicitaire introuvable.";
    private const string PlacementInactiveError = "Cet emplacement publicitaire n'est plus disponible.";

    private readonly IAdvertiserProfileRepository _profileRepository;
    private readonly IAdvertisingPlacementRepository _placementRepository;
    private readonly IAdCampaignRepository _campaignRepository;

    public CreateCampaignCommandHandler(
        IAdvertiserProfileRepository profileRepository, IAdvertisingPlacementRepository placementRepository,
        IAdCampaignRepository campaignRepository)
    {
        _profileRepository = profileRepository;
        _placementRepository = placementRepository;
        _campaignRepository = campaignRepository;
    }

    public async Task<Result<Guid>> Handle(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.AdvertiserUserId, cancellationToken);

        if (profile is null)
        {
            return Result<Guid>.Failure(NoProfileError, ErrorType.NotFound);
        }

        var placement = await _placementRepository.GetByIdAsync(command.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result<Guid>.Failure(PlacementNotFoundError, ErrorType.NotFound);
        }

        if (!placement.IsActive)
        {
            return Result<Guid>.Failure(PlacementInactiveError, ErrorType.Validation);
        }

        AdCampaign campaign;

        try
        {
            campaign = AdCampaign.Create(
                profile.Id, command.AdvertiserUserId, command.Name, command.Description, command.Objective, command.PlacementId,
                command.StartAtUtc, command.EndAtUtc, command.PricingModel, command.PriceRate, command.BudgetLimit,
                command.DailyBudgetLimit, command.TargetCity, command.TargetVehicleCategory, command.TargetDaysOfWeek,
                command.TargetStartHour, command.TargetEndHour, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        return Result<Guid>.Success(campaign.Id);
    }
}
