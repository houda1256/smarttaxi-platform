using SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferralCode;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Referrals.Queries;

public class GetMyReferralCodeQueryHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeReferralCodeGenerator _codeGenerator = new();
    private readonly GetMyReferralCodeQueryHandler _handler;

    public GetMyReferralCodeQueryHandlerTests()
    {
        _handler = new GetMyReferralCodeQueryHandler(_userRepository, _codeGenerator);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCodeYet_GeneratesAndPersistsOne()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new GetMyReferralCodeQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
        var reloaded = await _userRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.Equal(result.Value, reloaded!.ReferralCode);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasACode_ReturnsTheSameCodeEveryTime()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var first = await _handler.Handle(new GetMyReferralCodeQuery(user.Id), CancellationToken.None);
        var second = await _handler.Handle(new GetMyReferralCodeQuery(user.Id), CancellationToken.None);

        Assert.Equal(first.Value, second.Value);
    }
}
