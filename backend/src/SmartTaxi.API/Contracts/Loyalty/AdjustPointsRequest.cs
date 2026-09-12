namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record AdjustPointsRequest(Guid UserId, string PointType, int Points, string Reason);
