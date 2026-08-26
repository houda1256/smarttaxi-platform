using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsidePartnerProfile;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Queries;

public class GetMyRoadsidePartnerProfileQueryHandlerTests
{
    private readonly FakeRoadsidePartnerProfileRepository _profileRepository = new();
    private readonly GetMyRoadsidePartnerProfileQueryHandler _handler;

    public GetMyRoadsidePartnerProfileQueryHandlerTests()
    {
        _handler = new GetMyRoadsidePartnerProfileQueryHandler(_profileRepository);
    }

    [Fact]
    public async Task Handle_ExistingProfile_ReturnsIt()
    {
        var userId = Guid.NewGuid();
        var profile = RoadsidePartnerProfile.Register(userId, "Assistance Rapide", null, "12 rue X", "Tunis", null, null, null, null, DateTime.UtcNow);
        await _profileRepository.TryAddAsync(profile, CancellationToken.None);

        var result = await _handler.Handle(new GetMyRoadsidePartnerProfileQuery(userId), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_NoProfile_ReturnsNull()
    {
        var result = await _handler.Handle(new GetMyRoadsidePartnerProfileQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }
}
