using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Identity.DataRequests.Commands.SubmitPersonalDataRequest;

public sealed record SubmitPersonalDataRequestCommand(Guid UserId, PersonalDataRequestType RequestType) : ICommand<Result<Guid>>;
