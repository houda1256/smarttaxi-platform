using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerEmployees;

public sealed class GetBusinessCustomerEmployeesQueryHandler : IQueryHandler<GetBusinessCustomerEmployeesQuery, IReadOnlyCollection<BusinessCustomerEmployee>>
{
    private readonly IBusinessCustomerEmployeeRepository _employeeRepository;

    public GetBusinessCustomerEmployeesQueryHandler(IBusinessCustomerEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public Task<IReadOnlyCollection<BusinessCustomerEmployee>> Handle(GetBusinessCustomerEmployeesQuery query, CancellationToken cancellationToken) =>
        _employeeRepository.GetForBusinessCustomerAsync(query.BusinessCustomerId, cancellationToken);
}
