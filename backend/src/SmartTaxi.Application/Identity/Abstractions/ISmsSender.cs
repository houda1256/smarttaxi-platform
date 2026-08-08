namespace SmartTaxi.Application.Identity.Abstractions;

public interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken);
}
