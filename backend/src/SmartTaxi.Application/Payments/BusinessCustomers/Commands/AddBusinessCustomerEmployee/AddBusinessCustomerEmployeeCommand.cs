using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.AddBusinessCustomerEmployee;

public sealed record AddBusinessCustomerEmployeeCommand(
    Guid BusinessCustomerId, Guid UserId, BusinessEmployeeRole Role, decimal? RideBudgetPerMonth,
    string? AllowedVehicleCategories, TimeOnly? AllowedScheduleStart, TimeOnly? AllowedScheduleEnd, string? AllowedZones,
    decimal? PerRideLimit) : ICommand<Result<Guid>>;
