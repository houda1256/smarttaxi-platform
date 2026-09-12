using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITokenGenerator
{
    Task<string> GenerateToken(User user, Guid sessionId, CancellationToken cancellationToken);
}
