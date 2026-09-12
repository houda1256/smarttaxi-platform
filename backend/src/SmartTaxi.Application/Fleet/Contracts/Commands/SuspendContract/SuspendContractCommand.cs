using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.SuspendContract;

public sealed record SuspendContractCommand(Guid RequestingUserId, Guid ContractId) : ICommand<Result>;
