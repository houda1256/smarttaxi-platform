using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Events;

namespace SmartTaxi.Domain.Tests.Payments;

public class PaymentTests
{
    [Fact]
    public void Create_WithValidData_SetsPendingStatusAndGeneratesReference()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentMethod.Cash, 20m, 22.5m, "TND", DateTime.UtcNow);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.StartsWith("PAY-", payment.PaymentReference);
        Assert.Equal(22.5m, payment.FinalFareAmount);
        Assert.Equal("TND", payment.Currency);
        Assert.Equal(0m, payment.RefundedAmount);
        Assert.Single(payment.DomainEvents);
        Assert.IsType<PaymentCreated>(payment.DomainEvents.Single());
    }

    [Fact]
    public void Create_WithNegativeFinalFare_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentMethod.Cash, null, -5m, "TND", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithInvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentMethod.Cash, null, 20m, "TOOLONG", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithBlankRideNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(
            Guid.NewGuid(), "  ", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentMethod.Cash, null, 20m, "TND", DateTime.UtcNow));
    }

    [Fact]
    public void RemainingRefundableAmount_ReflectsFinalFareMinusRefunded()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentMethod.Card, null, 50m, "TND", DateTime.UtcNow);

        Assert.Equal(50m, payment.RemainingRefundableAmount);
    }
}
