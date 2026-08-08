using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.DeactivateBusinessCustomerEmployee;

public sealed record DeactivateBusinessCustomerEmployeeCommand(Guid EmployeeId) : ICommand<Result>;
