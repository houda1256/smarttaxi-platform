using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Professional.Commands.RejectProfessionalAccountRequest;

public sealed record RejectProfessionalAccountRequestCommand(
    Guid ReviewerId, Guid RequestId, string RejectionReason, string? ReviewComment) : ICommand<Result>;
