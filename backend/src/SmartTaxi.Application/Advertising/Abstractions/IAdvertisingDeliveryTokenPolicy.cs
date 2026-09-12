namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>Admin-configurable token lifetime — same "policy abstraction, never a hardcoded constant" convention as IAdvertisingMediaUploadPolicy/IDocumentUploadPolicy.</summary>
public interface IAdvertisingDeliveryTokenPolicy
{
    TimeSpan TokenLifetime { get; }
}
