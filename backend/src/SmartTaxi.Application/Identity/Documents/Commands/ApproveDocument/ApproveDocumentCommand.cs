using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.ApproveDocument;

public sealed record ApproveDocumentCommand(Guid ReviewerId, Guid DocumentId, string? ReviewComment) : ICommand<Result>;
