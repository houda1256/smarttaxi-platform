using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record RegisterBusinessCustomerRequest(
    string LegalName, string TaxIdentifier, string BillingAddress, string ContactPersonName, string ContactPersonEmail,
    string? ContactPersonPhone, DeferredPaymentTerm PaymentTerms, decimal CreditLimit);

public sealed record AddBusinessCustomerEmployeeRequest(
    Guid UserId, BusinessEmployeeRole Role, decimal? RideBudgetPerMonth, string? AllowedVehicleCategories,
    TimeOnly? AllowedScheduleStart, TimeOnly? AllowedScheduleEnd, string? AllowedZones, decimal? PerRideLimit);
