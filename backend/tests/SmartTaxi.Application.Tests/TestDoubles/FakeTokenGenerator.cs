using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public Task<string> GenerateToken(User user, Guid sessionId, CancellationToken cancellationToken)
        => Task.FromResult($"token-for-{user.Id}-session-{sessionId}");
}
