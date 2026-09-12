using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTwoFactorRecoveryCodeRepository : ITwoFactorRecoveryCodeRepository
{
    private readonly Dictionary<Guid, TwoFactorRecoveryCode> _codesById = new();

    public Task AddRangeAsync(IReadOnlyCollection<TwoFactorRecoveryCode> codes, CancellationToken cancellationToken)
    {
        foreach (var code in codes)
        {
            _codesById[code.Id] = code;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TwoFactorRecoveryCode>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TwoFactorRecoveryCode> codes = _codesById.Values
            .Where(c => c.UserId == userId && c.IsValid())
            .ToList();

        return Task.FromResult(codes);
    }

    public Task<bool> TryConsumeAsync(Guid codeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_codesById.TryGetValue(codeId, out var code) || !code.IsValid())
        {
            return Task.FromResult(false);
        }

        typeof(TwoFactorRecoveryCode).GetProperty(nameof(TwoFactorRecoveryCode.ConsumedAt))!.SetValue(code, utcNow);
        return Task.FromResult(true);
    }

    public Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var id in _codesById.Values.Where(c => c.UserId == userId).Select(c => c.Id).ToList())
        {
            _codesById.Remove(id);
        }

        return Task.CompletedTask;
    }

    public int Count => _codesById.Count;
}
