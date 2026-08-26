using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignMediaContent;

/// <summary>Never exposes the raw storage path/key to the caller — only streams the bytes through IFileStorageService.</summary>
public sealed class GetCampaignMediaContentQueryHandler : IQueryHandler<GetCampaignMediaContentQuery, Result<CampaignMediaContent>>
{
    private const string NotFoundError = "Média introuvable.";
    private const string NotOwnerError = "Vous ne pouvez consulter que les médias de vos propres campagnes.";

    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IFileStorageService _fileStorage;

    public GetCampaignMediaContentQueryHandler(
        ICampaignCreativeRepository creativeRepository, IAdCampaignRepository campaignRepository, IFileStorageService fileStorage)
    {
        _creativeRepository = creativeRepository;
        _campaignRepository = campaignRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<CampaignMediaContent>> Handle(GetCampaignMediaContentQuery query, CancellationToken cancellationToken)
    {
        var creative = await _creativeRepository.GetByIdAsync(query.CreativeId, cancellationToken);

        if (creative is null)
        {
            return Result<CampaignMediaContent>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var campaign = await _campaignRepository.GetByIdAsync(creative.CampaignId, cancellationToken);

        if (campaign is null || campaign.AdvertiserUserId != query.RequestingUserId)
        {
            return Result<CampaignMediaContent>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var content = await _fileStorage.OpenReadAsync(creative.StorageKey, cancellationToken);
        return Result<CampaignMediaContent>.Success(new CampaignMediaContent(content, creative.MimeType, creative.DisplayName));
    }
}
