namespace SmartTaxi.API.Contracts.Fleet.Assignments;

public sealed record CompleteAssignmentRequest(int MileageStart, int MileageEnd, int RideCount, decimal? RevenueGenerated, int IncidentCount);
