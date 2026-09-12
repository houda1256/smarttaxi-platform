using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.SuspendBusinessCustomer;

public sealed record SuspendBusinessCustomerCommand(Guid BusinessCustomerId) : ICommand<Result>;
