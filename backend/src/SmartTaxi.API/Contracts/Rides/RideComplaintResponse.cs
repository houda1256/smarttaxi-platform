using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.API.Contracts.Rides;

public sealed record RideComplaintResponse(
    Guid Id, Guid RideId, Guid ComplainantUserId, Guid ConcernedUserId, string Category, string Description,
    string Status, string? Resolution, DateTime CreatedAt, DateTime? ResolvedAt)
{
    public static RideComplaintResponse FromEntity(RideComplaint complaint) => new(
        complaint.Id, complaint.RideId, complaint.ComplainantUserId, complaint.ConcernedUserId,
        complaint.Category.ToString(), complaint.Description, complaint.Status.ToString(), complaint.Resolution,
        complaint.CreatedAt, complaint.ResolvedAt);
}
