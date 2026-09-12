using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.MarkGroupedInvoicePaid;

public sealed record MarkGroupedInvoicePaidCommand(Guid GroupedInvoiceId) : ICommand<Result>;
