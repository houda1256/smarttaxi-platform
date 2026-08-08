using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.SuspendDocument;

public sealed record SuspendDocumentCommand(Guid ReviewerId, Guid DocumentId, string? ReviewComment) : ICommand<Result>;
