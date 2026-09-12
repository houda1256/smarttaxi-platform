using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetAllBusinessCustomers;

public sealed record GetAllBusinessCustomersQuery(
    BusinessCustomerStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<BusinessCustomer>>;
