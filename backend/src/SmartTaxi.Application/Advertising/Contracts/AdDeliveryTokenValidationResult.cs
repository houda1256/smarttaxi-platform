namespace SmartTaxi.Application.Advertising.Contracts;

/// <summary>DeliveryId is the only thing a valid token yields to the caller — it is never the client-supplied idempotency key itself (that is derived from it deterministically inside the recording handler), so the client can never choose or influence it.</summary>
public sealed record AdDeliveryTokenValidationResult(bool IsValid, Guid? DeliveryId, string? FailureReason)
{
    public static AdDeliveryTokenValidationResult Valid(Guid deliveryId) => new(true, deliveryId, null);

    public static AdDeliveryTokenValidationResult Invalid(string reason) => new(false, null, reason);
}
