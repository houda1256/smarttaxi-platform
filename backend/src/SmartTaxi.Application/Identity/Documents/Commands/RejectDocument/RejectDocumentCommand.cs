using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.RejectDocument;

public sealed record RejectDocumentCommand(
    Guid ReviewerId, Guid DocumentId, string RejectionReason, string? ReviewComment) : ICommand<Result>;
