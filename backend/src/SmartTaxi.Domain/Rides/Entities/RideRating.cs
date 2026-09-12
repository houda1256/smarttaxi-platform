using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>Uniqueness of (RideId, ReviewerId) is enforced by a DB unique index — the entity itself only validates its own shape.</summary>
public sealed class RideRating : AggregateRoot
{
    public Guid RideId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid ReviewedUserId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public string? Tags { get; private set; }
    public bool IsReported { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private RideRating()
    {
    }

    private RideRating(Guid rideId, Guid reviewerId, Guid reviewedUserId, int score, string? comment, string? tags, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        ReviewerId = reviewerId;
        ReviewedUserId = reviewedUserId;
        Score = score;
        Comment = comment;
        Tags = tags;
        CreatedAt = utcNow;

        RaiseDomainEvent(new RideRatingSubmitted(Id, rideId, reviewerId, reviewedUserId, utcNow));
    }

    public static RideRating Submit(Guid rideId, Guid reviewerId, Guid reviewedUserId, int score, string? comment, string? tags, DateTime utcNow)
    {
        if (reviewerId == reviewedUserId)
        {
            throw new ArgumentException("Un utilisateur ne peut pas s'auto-évaluer.");
        }

        if (score is < 1 or > 5)
        {
            throw new ArgumentException("La note doit être comprise entre 1 et 5.");
        }

        return new RideRating(rideId, reviewerId, reviewedUserId, score, comment, tags, utcNow);
    }
}
