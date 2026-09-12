using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.CancelDocument;

public sealed record CancelDocumentCommand(Guid UserId, Guid DocumentId) : ICommand<Result>;
