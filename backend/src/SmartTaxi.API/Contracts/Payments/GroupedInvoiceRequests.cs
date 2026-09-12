using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record GroupedInvoiceRideLineRequest(Guid RideId, string RideNumber, decimal Amount);

public sealed record GenerateGroupedInvoiceRequest(
    Guid BusinessCustomerId, GroupedInvoicePeriodType PeriodType, DateOnly PeriodStart, DateOnly PeriodEnd, string Currency,
    DateTime DueDate, IReadOnlyCollection<GroupedInvoiceRideLineRequest> Rides);
