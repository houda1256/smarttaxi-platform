using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Notifications.Repositories;

internal sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        await _context.NotificationTemplates.AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        _context.NotificationTemplates.Update(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken) =>
        _context.NotificationTemplates.FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task<NotificationTemplate?> GetAsync(
        string templateKey, NotificationChannel channel, Language language, CancellationToken cancellationToken) =>
        _context.NotificationTemplates.FirstOrDefaultAsync(
            template => template.TemplateKey == templateKey && template.Channel == channel && template.Language == language,
            cancellationToken);

    public async Task<IReadOnlyCollection<NotificationTemplate>> GetAllForKeyAsync(string templateKey, CancellationToken cancellationToken) =>
        await _context.NotificationTemplates.Where(template => template.TemplateKey == templateKey).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.NotificationTemplates.OrderBy(template => template.TemplateKey).ToListAsync(cancellationToken);
}
