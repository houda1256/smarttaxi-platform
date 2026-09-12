using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.GenerateGroupedInvoice;

public sealed record GroupedInvoiceRideLine(Guid RideId, string RideNumber, decimal Amount);

public sealed record GenerateGroupedInvoiceCommand(
    Guid BusinessCustomerId, GroupedInvoicePeriodType PeriodType, DateOnly PeriodStart, DateOnly PeriodEnd,
    string Currency, DateTime DueDate, IReadOnlyCollection<GroupedInvoiceRideLine> Rides) : ICommand<Result<Guid>>;
