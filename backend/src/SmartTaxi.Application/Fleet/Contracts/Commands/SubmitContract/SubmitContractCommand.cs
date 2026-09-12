using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;

public sealed record SubmitContractCommand(Guid RequestingUserId, Guid ContractId) : ICommand<Result>;
