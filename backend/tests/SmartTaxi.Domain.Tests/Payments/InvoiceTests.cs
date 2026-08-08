using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Tests.Payments;

public class InvoiceTests
{
    [Fact]
    public void Generate_WithValidData_ComputesTotalAndRaisesEvent()
    {
        var invoice = Invoice.Generate(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20m, 2m, "TND",
            PaymentMethod.Cash, DateTime.UtcNow);

        Assert.Equal(22m, invoice.TotalAmount);
        Assert.StartsWith("INV-", invoice.InvoiceNumber);
        Assert.Null(invoice.PdfStorageKey);
        Assert.Single(invoice.DomainEvents);
        Assert.IsType<InvoiceGenerated>(invoice.DomainEvents.Single());
    }

    [Fact]
    public void Generate_WithNegativeSubtotal_Throws()
    {
        Assert.Throws<ArgumentException>(() => Invoice.Generate(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1m, 0m, "TND",
            PaymentMethod.Cash, DateTime.UtcNow));
    }

    [Fact]
    public void AttachPdf_WithValidKey_SetsPdfStorageKey()
    {
        var invoice = Invoice.Generate(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20m, 2m, "TND",
            PaymentMethod.Cash, DateTime.UtcNow);

        invoice.AttachPdf("invoices/some-key.pdf");

        Assert.Equal("invoices/some-key.pdf", invoice.PdfStorageKey);
    }
}
