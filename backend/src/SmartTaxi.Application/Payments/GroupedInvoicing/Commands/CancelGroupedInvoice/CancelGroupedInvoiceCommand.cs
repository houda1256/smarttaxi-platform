using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.CancelGroupedInvoice;

public sealed record CancelGroupedInvoiceCommand(Guid GroupedInvoiceId) : ICommand<Result>;
