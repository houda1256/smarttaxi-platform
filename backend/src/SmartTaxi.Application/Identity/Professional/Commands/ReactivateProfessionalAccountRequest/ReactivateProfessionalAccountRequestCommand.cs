using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Professional.Commands.ReactivateProfessionalAccountRequest;

public sealed record ReactivateProfessionalAccountRequestCommand(Guid ReviewerId, Guid RequestId, string? ReviewComment)
    : ICommand<Result>;
