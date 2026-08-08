using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Professional.Commands.SuspendProfessionalAccountRequest;

public sealed record SuspendProfessionalAccountRequestCommand(Guid ReviewerId, Guid RequestId, string? ReviewComment)
    : ICommand<Result>;
