using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.API.Contracts.Rides;

public sealed record RideRatingResponse(
    Guid Id, Guid RideId, Guid ReviewerId, Guid ReviewedUserId, int Score, string? Comment, string? Tags,
    bool IsReported, DateTime CreatedAt)
{
    public static RideRatingResponse FromEntity(RideRating rating) => new(
        rating.Id, rating.RideId, rating.ReviewerId, rating.ReviewedUserId, rating.Score, rating.Comment, rating.Tags,
        rating.IsReported, rating.CreatedAt);
}
