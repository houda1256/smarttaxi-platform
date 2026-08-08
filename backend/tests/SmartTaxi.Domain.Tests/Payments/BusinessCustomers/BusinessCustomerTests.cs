using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Domain.Tests.Payments.BusinessCustomers;

public class BusinessCustomerTests
{
    [Fact]
    public void Register_WithValidData_StartsWithZeroCreditUsage()
    {
        var customer = BusinessCustomer.Register(
            "Acme SARL", "TAX123", "12 Rue de Tunis", "Jane Doe", "jane@acme.tn", null, DeferredPaymentTerm.Days30, 1000m,
            DateTime.UtcNow);

        Assert.Equal(0m, customer.CurrentCreditUsage);
        Assert.Equal(1000m, customer.RemainingCredit);
    }

    [Fact]
    public void Register_WithBlankLegalName_Throws()
    {
        Assert.Throws<ArgumentException>(() => BusinessCustomer.Register(
            " ", "TAX123", "Address", "Jane", "jane@acme.tn", null, DeferredPaymentTerm.Immediate, 0m, DateTime.UtcNow));
    }

    [Fact]
    public void Register_WithNegativeCreditLimit_Throws()
    {
        Assert.Throws<ArgumentException>(() => BusinessCustomer.Register(
            "Acme", "TAX123", "Address", "Jane", "jane@acme.tn", null, DeferredPaymentTerm.Immediate, -1m, DateTime.UtcNow));
    }
}
