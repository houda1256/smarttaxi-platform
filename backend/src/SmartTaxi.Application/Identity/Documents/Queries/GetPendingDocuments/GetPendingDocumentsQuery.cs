using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetPendingDocuments;

public sealed record GetPendingDocumentsQuery : IQuery<IReadOnlyCollection<UserDocumentSummary>>;
