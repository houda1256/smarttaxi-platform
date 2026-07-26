using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}
