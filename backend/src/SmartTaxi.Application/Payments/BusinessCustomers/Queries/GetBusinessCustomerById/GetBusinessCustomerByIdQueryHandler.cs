using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerById;

public sealed class GetBusinessCustomerByIdQueryHandler : IQueryHandler<GetBusinessCustomerByIdQuery, Result<BusinessCustomer>>
{
    private const string NotFoundError = "Client entreprise introuvable.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public GetBusinessCustomerByIdQueryHandler(IBusinessCustomerRepository businessCustomerRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result<BusinessCustomer>> Handle(GetBusinessCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        var customer = await _businessCustomerRepository.GetByIdAsync(query.BusinessCustomerId, cancellationToken);

        return customer is null
            ? Result<BusinessCustomer>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<BusinessCustomer>.Success(customer);
    }
}
