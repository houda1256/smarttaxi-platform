using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.DataRequests.Commands.ProcessPersonalDataRequest;

public sealed record ProcessPersonalDataRequestCommand(
    Guid ProcessedBy, Guid RequestId, bool Approve, string? ProcessingNotes) : ICommand<Result>;
