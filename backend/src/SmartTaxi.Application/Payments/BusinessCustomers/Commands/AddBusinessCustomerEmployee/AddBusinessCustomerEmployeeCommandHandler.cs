using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.AddBusinessCustomerEmployee;

public sealed class AddBusinessCustomerEmployeeCommandHandler : ICommandHandler<AddBusinessCustomerEmployeeCommand, Result<Guid>>
{
    private const string CustomerNotFoundError = "Client entreprise introuvable.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;
    private readonly IBusinessCustomerEmployeeRepository _employeeRepository;

    public AddBusinessCustomerEmployeeCommandHandler(
        IBusinessCustomerRepository businessCustomerRepository, IBusinessCustomerEmployeeRepository employeeRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<Guid>> Handle(AddBusinessCustomerEmployeeCommand command, CancellationToken cancellationToken)
    {
        var customer = await _businessCustomerRepository.GetByIdAsync(command.BusinessCustomerId, cancellationToken);

        if (customer is null)
        {
            return Result<Guid>.Failure(CustomerNotFoundError, ErrorType.NotFound);
        }

        BusinessCustomerEmployee employee;

        try
        {
            employee = new BusinessCustomerEmployee(
                command.BusinessCustomerId, command.UserId, command.Role, command.RideBudgetPerMonth,
                command.AllowedVehicleCategories, command.AllowedScheduleStart, command.AllowedScheduleEnd,
                command.AllowedZones, command.PerRideLimit, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _employeeRepository.AddAsync(employee, cancellationToken);

        return Result<Guid>.Success(employee.Id);
    }
}
