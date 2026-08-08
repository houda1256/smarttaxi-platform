using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.ActivateContract;

public sealed record ActivateContractCommand(Guid RequestingUserId, Guid ContractId) : ICommand<Result>;
