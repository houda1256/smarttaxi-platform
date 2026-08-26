using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Abstractions;

public interface INotificationTemplateRepository
{
    Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken);

    Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetAsync(string templateKey, NotificationChannel channel, Language language, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationTemplate>> GetAllForKeyAsync(string templateKey, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken);
}
