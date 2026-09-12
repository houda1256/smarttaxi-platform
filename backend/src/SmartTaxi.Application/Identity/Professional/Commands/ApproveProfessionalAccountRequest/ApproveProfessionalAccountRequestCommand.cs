using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Professional.Commands.ApproveProfessionalAccountRequest;

public sealed record ApproveProfessionalAccountRequestCommand(Guid ReviewerId, Guid RequestId, string? ReviewComment)
    : ICommand<Result>;
