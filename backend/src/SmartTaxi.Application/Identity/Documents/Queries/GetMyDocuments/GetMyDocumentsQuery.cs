using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetMyDocuments;

public sealed record GetMyDocumentsQuery(Guid UserId) : IQuery<IReadOnlyCollection<UserDocumentSummary>>;
