using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.UploadCampaignMedia;

public sealed record UploadCampaignMediaCommand(
    Guid CampaignId, Guid RequestingUserId, AdMediaType MediaType, string DeclaredMimeType, string OriginalFileName, Stream Content)
    : ICommand<Result<Guid>>;
