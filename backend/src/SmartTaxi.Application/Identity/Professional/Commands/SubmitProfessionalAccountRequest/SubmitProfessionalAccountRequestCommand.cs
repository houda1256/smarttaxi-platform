using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Professional.Commands.SubmitProfessionalAccountRequest;

public sealed record SubmitProfessionalAccountRequestCommand(Guid UserId, UserRole Role) : ICommand<Result<Guid>>;
