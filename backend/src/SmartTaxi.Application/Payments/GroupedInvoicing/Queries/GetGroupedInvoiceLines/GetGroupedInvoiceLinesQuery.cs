using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceLines;

public sealed record GetGroupedInvoiceLinesQuery(Guid GroupedInvoiceId) : IQuery<IReadOnlyCollection<GroupedInvoiceLine>>;
