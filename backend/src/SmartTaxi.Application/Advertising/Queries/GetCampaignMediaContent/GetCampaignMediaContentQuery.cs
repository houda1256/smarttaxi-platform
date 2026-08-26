using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignMediaContent;

public sealed record GetCampaignMediaContentQuery(Guid CreativeId, Guid RequestingUserId) : IQuery<Result<CampaignMediaContent>>;

public sealed record CampaignMediaContent(Stream Content, string MimeType, string FileName);
