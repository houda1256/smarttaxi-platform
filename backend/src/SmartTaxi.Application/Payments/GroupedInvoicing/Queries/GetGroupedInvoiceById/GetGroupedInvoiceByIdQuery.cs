using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceById;

public sealed record GetGroupedInvoiceByIdQuery(Guid GroupedInvoiceId) : IQuery<Result<GroupedInvoice>>;
