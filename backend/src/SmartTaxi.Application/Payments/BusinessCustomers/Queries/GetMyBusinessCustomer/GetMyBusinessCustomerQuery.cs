using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetMyBusinessCustomer;

public sealed record GetMyBusinessCustomerQuery(Guid UserId) : IQuery<Result<BusinessCustomer>>;
