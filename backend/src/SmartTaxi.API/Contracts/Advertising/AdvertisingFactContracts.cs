namespace SmartTaxi.API.Contracts.Advertising;

public sealed record RequestAdDeliveryRequest(Guid PlacementId);

public sealed record AdDeliveryTokenResponse(string DeliveryToken);

public sealed record RecordImpressionRequest(Guid PlacementId, string DeliveryToken, DateTime OccurredAtUtc);

public sealed record RecordClickRequest(Guid PlacementId, Guid? ImpressionId, string DeliveryToken, DateTime OccurredAtUtc);
