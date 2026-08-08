namespace SmartTaxi.Application.Identity.Abstractions;

public interface IOtpGenerator
{
    /// <summary>Generates a new random numeric one-time code of the given length.</summary>
    string Generate(int digits);
}
