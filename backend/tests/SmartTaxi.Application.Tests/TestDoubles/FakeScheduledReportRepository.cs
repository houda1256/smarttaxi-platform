using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of the real repository's atomic claim/finalize guards — same reflection SetProperty convention as every other Fake*Repository.</summary>
public sealed class FakeScheduledReportRepository : IScheduledReportRepository
{
    private readonly Dictionary<Guid, ScheduledReportDefinition> _definitions = new();

    public Task<bool> TryAddAsync(ScheduledReportDefinition definition, CancellationToken cancellationToken)
    {
        _definitions[definition.Id] = definition;
        return Task.FromResult(true);
    }

    public Task<ScheduledReportDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_definitions.GetValueOrDefault(id));

    public Task<PagedResult<ScheduledReportDefinition>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _definitions.Values.ToList();
        return Task.FromResult(new PagedResult<ScheduledReportDefinition>(items, items.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<Guid>> GetDueIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var staleBefore = utcNow - IScheduledReportRepository.StaleClaimThreshold;

        IReadOnlyCollection<Guid> ids = _definitions.Values
            .Where(d => d.IsActive && d.NextRunAtUtc <= utcNow
                && (d.ProcessingClaimedAtUtc is null || d.ProcessingClaimedAtUtc < staleBefore))
            .Select(d => d.Id)
            .ToList();

        return Task.FromResult(ids);
    }

    public Task<bool> TryClaimAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var staleBefore = utcNow - IScheduledReportRepository.StaleClaimThreshold;

        if (!_definitions.TryGetValue(id, out var definition) || !definition.IsActive || definition.NextRunAtUtc > utcNow
            || (definition.ProcessingClaimedAtUtc is not null && definition.ProcessingClaimedAtUtc >= staleBefore))
        {
            return Task.FromResult(false);
        }

        SetProperty(definition, nameof(ScheduledReportDefinition.ProcessingClaimedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryFinalizeAsync(
        Guid id, DateTime claimedAtUtc, DateTime nextRunAtUtc, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetValue(id, out var definition) || definition.ProcessingClaimedAtUtc != claimedAtUtc)
        {
            return Task.FromResult(false);
        }

        SetProperty(definition, nameof(ScheduledReportDefinition.ProcessingClaimedAtUtc), null);
        SetProperty(definition, nameof(ScheduledReportDefinition.NextRunAtUtc), nextRunAtUtc);
        SetProperty(definition, nameof(ScheduledReportDefinition.LastProcessedAtUtc), utcNow);
        SetProperty(definition, nameof(ScheduledReportDefinition.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateAsync(
        Guid id, ScheduledReportCategory category, ScheduledReportFrequency frequency, Guid recipientUserId, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetValue(id, out var definition))
        {
            return Task.FromResult(false);
        }

        SetProperty(definition, nameof(ScheduledReportDefinition.Category), category);
        SetProperty(definition, nameof(ScheduledReportDefinition.Frequency), frequency);
        SetProperty(definition, nameof(ScheduledReportDefinition.RecipientUserId), recipientUserId);
        SetProperty(definition, nameof(ScheduledReportDefinition.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryDeactivateAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetValue(id, out var definition))
        {
            return Task.FromResult(false);
        }

        SetProperty(definition, nameof(ScheduledReportDefinition.IsActive), false);
        SetProperty(definition, nameof(ScheduledReportDefinition.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(ScheduledReportDefinition definition, string propertyName, object? value) =>
        typeof(ScheduledReportDefinition).GetProperty(propertyName)!.SetValue(definition, value);
}
