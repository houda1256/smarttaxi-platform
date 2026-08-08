using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetDocumentByIdAdmin;

public sealed record GetDocumentByIdAdminQuery(Guid RequestedByUserId, Guid DocumentId, string? IpAddress)
    : IQuery<Result<UserDocumentSummary>>;
