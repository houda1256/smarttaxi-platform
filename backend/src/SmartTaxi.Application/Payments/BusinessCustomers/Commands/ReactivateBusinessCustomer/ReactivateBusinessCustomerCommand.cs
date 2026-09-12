using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.ReactivateBusinessCustomer;

public sealed record ReactivateBusinessCustomerCommand(Guid BusinessCustomerId) : ICommand<Result>;
