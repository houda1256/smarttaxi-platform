using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetDocumentContentAdmin;

public sealed record GetDocumentContentAdminQuery(Guid RequestedByUserId, Guid DocumentId, string? IpAddress)
    : IQuery<Result<DocumentContentResult>>;
