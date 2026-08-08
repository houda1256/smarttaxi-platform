using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Domain.Tests.Payments.GroupedInvoicing;

public class GroupedInvoiceTests
{
    [Fact]
    public void Generate_ComputesTotalFromSubtotalAndTax()
    {
        var invoice = GroupedInvoice.Generate(
            Guid.NewGuid(), GroupedInvoicePeriodType.Weekly, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 100m, 10m,
            "TND", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        Assert.Equal(110m, invoice.TotalAmount);
        Assert.StartsWith("GINV-", invoice.InvoiceNumber);
        Assert.Equal(GroupedInvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void Generate_WithDueDateBeforeIssueDate_Throws()
    {
        Assert.Throws<ArgumentException>(() => GroupedInvoice.Generate(
            Guid.NewGuid(), GroupedInvoicePeriodType.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 100m, 10m,
            "TND", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void GroupedInvoiceLine_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GroupedInvoiceLine(Guid.NewGuid(), Guid.NewGuid(), "RD-1", -5m, DateTime.UtcNow));
    }
}
