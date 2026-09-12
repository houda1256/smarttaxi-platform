namespace SmartTaxi.Application.Payments.Abstractions;

/// <summary>Abstraction only, per instructions — no real PDF rendering library is integrated; the Infrastructure implementation is a documented dev stub.</summary>
public interface IReceiptPdfGenerator
{
    Task<string> GenerateAsync(ReceiptPdfData data, CancellationToken cancellationToken);
}

public sealed record ReceiptPdfData(string ReceiptNumber, string PaymentReference, decimal Amount, string Currency, DateTime IssuedAt);
