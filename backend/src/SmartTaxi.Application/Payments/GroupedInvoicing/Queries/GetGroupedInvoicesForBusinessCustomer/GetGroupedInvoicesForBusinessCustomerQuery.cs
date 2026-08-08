using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoicesForBusinessCustomer;

public sealed record GetGroupedInvoicesForBusinessCustomerQuery(
    Guid BusinessCustomerId, GroupedInvoiceStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<GroupedInvoice>>;
