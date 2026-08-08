using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.TerminateContract;

public sealed record TerminateContractCommand(Guid RequestingUserId, Guid ContractId) : ICommand<Result>;
