using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly Dictionary<Guid, NotificationTemplate> _templates = new();

    public Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        _templates[template.Id] = template;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        _templates[template.Id] = template;
        return Task.CompletedTask;
    }

    public Task<NotificationTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult(_templates.GetValueOrDefault(templateId));

    public Task<NotificationTemplate?> GetAsync(
        string templateKey, NotificationChannel channel, Language language, CancellationToken cancellationToken) =>
        Task.FromResult(_templates.Values.FirstOrDefault(
            t => t.TemplateKey == templateKey && t.Channel == channel && t.Language == language));

    public Task<IReadOnlyCollection<NotificationTemplate>> GetAllForKeyAsync(string templateKey, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<NotificationTemplate>>(
            _templates.Values.Where(t => t.TemplateKey == templateKey).ToList());

    public Task<IReadOnlyCollection<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<NotificationTemplate>>(_templates.Values.ToList());
}
