using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.CloseBusinessCustomer;

public sealed record CloseBusinessCustomerCommand(Guid BusinessCustomerId) : ICommand<Result>;
