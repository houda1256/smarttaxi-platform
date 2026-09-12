using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetAllBusinessCustomers;

public sealed class GetAllBusinessCustomersQueryHandler : IQueryHandler<GetAllBusinessCustomersQuery, PagedResult<BusinessCustomer>>
{
    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public GetAllBusinessCustomersQueryHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public Task<PagedResult<BusinessCustomer>> Handle(GetAllBusinessCustomersQuery query, CancellationToken cancellationToken) =>
        _businessCustomerRepository.GetAllAsync(query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
