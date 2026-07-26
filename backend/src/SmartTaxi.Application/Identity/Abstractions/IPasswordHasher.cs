namespace SmartTaxi.Application.Identity.Abstractions;

public interface IPasswordHasher
{
    string Hash(string rawPassword);

    bool Verify(string hashedPassword, string providedPassword);
}
