using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.ExpireDocuments;

/// <summary>System-wide, idempotent sweep — no target document, safe to call repeatedly.</summary>
public sealed record ExpireDocumentsCommand : ICommand<Result<ExpireDocumentsResult>>;
