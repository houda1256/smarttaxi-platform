using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerEmployees;

public sealed record GetBusinessCustomerEmployeesQuery(Guid BusinessCustomerId) : IQuery<IReadOnlyCollection<BusinessCustomerEmployee>>;
