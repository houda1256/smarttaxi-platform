using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record BusinessCustomerResponse(
    Guid Id,
    string LegalName,
    string TaxIdentifier,
    string BillingAddress,
    string ContactPersonName,
    string ContactPersonEmail,
    string? ContactPersonPhone,
    string PaymentTerms,
    decimal CreditLimit,
    decimal CurrentCreditUsage,
    decimal RemainingCredit,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static BusinessCustomerResponse FromEntity(BusinessCustomer customer) => new(
        customer.Id, customer.LegalName, customer.TaxIdentifier, customer.BillingAddress, customer.ContactPersonName,
        customer.ContactPersonEmail, customer.ContactPersonPhone, customer.PaymentTerms.ToString(), customer.CreditLimit,
        customer.CurrentCreditUsage, customer.RemainingCredit, customer.Status.ToString(), customer.CreatedAt, customer.UpdatedAt);
}

public sealed record BusinessCustomerEmployeeResponse(
    Guid Id,
    Guid BusinessCustomerId,
    Guid UserId,
    string Role,
    decimal? RideBudgetPerMonth,
    string? AllowedVehicleCategories,
    TimeOnly? AllowedScheduleStart,
    TimeOnly? AllowedScheduleEnd,
    string? AllowedZones,
    decimal? PerRideLimit,
    bool IsActive,
    DateTime CreatedAt)
{
    public static BusinessCustomerEmployeeResponse FromEntity(BusinessCustomerEmployee employee) => new(
        employee.Id, employee.BusinessCustomerId, employee.UserId, employee.Role.ToString(), employee.RideBudgetPerMonth,
        employee.AllowedVehicleCategories, employee.AllowedScheduleStart, employee.AllowedScheduleEnd, employee.AllowedZones,
        employee.PerRideLimit, employee.IsActive, employee.CreatedAt);
}
