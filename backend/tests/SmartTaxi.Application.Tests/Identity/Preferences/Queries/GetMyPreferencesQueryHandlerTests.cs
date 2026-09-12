using SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Tests.Identity.Preferences.Queries;

public class GetMyPreferencesQueryHandlerTests
{
    private readonly FakeUserPreferencesRepository _repository = new();
    private readonly GetMyPreferencesQueryHandler _handler;

    public GetMyPreferencesQueryHandlerTests()
    {
        _handler = new GetMyPreferencesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNoPreferencesExistYet_ReturnsDefaultsWithoutWritingAnything()
    {
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(new GetMyPreferencesQuery(userId), CancellationToken.None);

        Assert.Equal(Language.French, result.Language);
        Assert.Equal("UTC", result.Timezone);
        Assert.Null(await _repository.GetByUserIdAsync(userId, CancellationToken.None));
    }
}
