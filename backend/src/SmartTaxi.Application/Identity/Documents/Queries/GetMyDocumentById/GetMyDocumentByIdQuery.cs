using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentById;

public sealed record GetMyDocumentByIdQuery(Guid UserId, Guid DocumentId) : IQuery<Result<UserDocumentSummary>>;
