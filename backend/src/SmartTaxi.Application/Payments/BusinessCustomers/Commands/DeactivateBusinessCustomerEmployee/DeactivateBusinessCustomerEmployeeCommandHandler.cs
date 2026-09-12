using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.DeactivateBusinessCustomerEmployee;

public sealed class DeactivateBusinessCustomerEmployeeCommandHandler : ICommandHandler<DeactivateBusinessCustomerEmployeeCommand, Result>
{
    private const string NotFoundError = "Employé introuvable.";
    private const string AlreadyInactiveError = "Cet employé est déjà inactif.";

    private readonly IBusinessCustomerEmployeeRepository _employeeRepository;

    public DeactivateBusinessCustomerEmployeeCommandHandler(IBusinessCustomerEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(DeactivateBusinessCustomerEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var deactivated = await _employeeRepository.TryDeactivateAsync(command.EmployeeId, DateTime.UtcNow, cancellationToken);

        return deactivated ? Result.Success() : Result.Failure(AlreadyInactiveError, ErrorType.Conflict);
    }
}
