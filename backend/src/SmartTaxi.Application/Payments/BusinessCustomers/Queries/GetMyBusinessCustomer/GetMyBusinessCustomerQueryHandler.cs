using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetMyBusinessCustomer;

public sealed class GetMyBusinessCustomerQueryHandler : IQueryHandler<GetMyBusinessCustomerQuery, Result<BusinessCustomer>>
{
    private const string NotMemberError = "Aucun compte entreprise associé à cet utilisateur.";
    private const string NotFoundError = "Client entreprise introuvable.";

    private readonly IBusinessCustomerEmployeeRepository _employeeRepository;
    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public GetMyBusinessCustomerQueryHandler(
        IBusinessCustomerEmployeeRepository employeeRepository, IBusinessCustomerRepository businessCustomerRepository)
    {
        _employeeRepository = employeeRepository;
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result<BusinessCustomer>> Handle(GetMyBusinessCustomerQuery query, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetActiveByUserIdAsync(query.UserId, cancellationToken);

        if (employee is null)
        {
            return Result<BusinessCustomer>.Failure(NotMemberError, ErrorType.NotFound);
        }

        var customer = await _businessCustomerRepository.GetByIdAsync(employee.BusinessCustomerId, cancellationToken);

        return customer is null
            ? Result<BusinessCustomer>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<BusinessCustomer>.Success(customer);
    }
}
