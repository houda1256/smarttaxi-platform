using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerById;

public sealed record GetBusinessCustomerByIdQuery(Guid BusinessCustomerId) : IQuery<Result<BusinessCustomer>>;
