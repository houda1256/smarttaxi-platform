using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Professional.Entities;

public class ProfessionalAccountRequestTests
{
    [Fact]
    public void Constructor_SetsInitialStateAsPendingReview()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var request = new ProfessionalAccountRequest(userId, UserRole.Driver, utcNow);

        Assert.Equal(userId, request.UserId);
        Assert.Equal(UserRole.Driver, request.Role);
        Assert.Equal(ProfessionalAccountStatus.PendingReview, request.Status);
        Assert.Equal(utcNow, request.CreatedAt);
        Assert.Null(request.ReviewedBy);
        Assert.Null(request.ReviewedAt);
    }
}
