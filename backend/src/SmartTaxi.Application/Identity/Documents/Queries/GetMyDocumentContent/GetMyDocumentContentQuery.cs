using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentContent;

public sealed record GetMyDocumentContentQuery(Guid UserId, Guid DocumentId, string? IpAddress)
    : IQuery<Result<DocumentContentResult>>;
