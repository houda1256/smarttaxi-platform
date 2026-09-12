using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideRatingTests
{
    [Fact]
    public void Submit_Succeeds_WithValidScore()
    {
        var rating = RideRating.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, "Great ride", null, DateTime.UtcNow);

        Assert.Equal(5, rating.Score);
        Assert.Single(rating.DomainEvents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Submit_WithOutOfRangeScore_Throws(int score)
    {
        Assert.Throws<ArgumentException>(() =>
            RideRating.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), score, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Submit_WithSameReviewerAndReviewed_Throws()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => RideRating.Submit(Guid.NewGuid(), userId, userId, 5, null, null, DateTime.UtcNow));
    }
}
