using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Domain.Tests.Payments;

/// <summary>Covers the Phase 5B additive fields on the already-completed Invoice entity — must not affect Phase 5A callers that omit them.</summary>
public class InvoiceTaxSnapshotTests
{
    [Fact]
    public void Generate_WithoutTaxRuleArguments_LeavesSnapshotFieldsNull()
    {
        var invoice = Invoice.Generate(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, 0m, "TND",
            PaymentMethod.Cash, DateTime.UtcNow);

        Assert.Null(invoice.TaxRuleId);
        Assert.Null(invoice.TaxRuleName);
        Assert.Null(invoice.TaxRateApplied);
    }

    [Fact]
    public void Generate_WithTaxRuleArguments_SnapshotsThem()
    {
        var taxRuleId = Guid.NewGuid();

        var invoice = Invoice.Generate(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, 19m, "TND",
            PaymentMethod.Card, DateTime.UtcNow, taxRuleId, "TVA", 19m);

        Assert.Equal(taxRuleId, invoice.TaxRuleId);
        Assert.Equal("TVA", invoice.TaxRuleName);
        Assert.Equal(19m, invoice.TaxRateApplied);
        Assert.Equal(119m, invoice.TotalAmount);
    }
}
