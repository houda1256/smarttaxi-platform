using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetOverdueGroupedInvoices;

public sealed record GetOverdueGroupedInvoicesQuery(DateTime AsOfUtc) : IQuery<IReadOnlyCollection<GroupedInvoice>>;
