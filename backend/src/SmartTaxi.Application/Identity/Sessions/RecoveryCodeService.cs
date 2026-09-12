using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Identity.Sessions;

/// <summary>
/// Shared recovery-code generation/verification logic, used by both 2FA
/// confirmation (first batch) and explicit regeneration (fresh batch), and by
/// both the login challenge and the "disable 2FA" strong-reauth check (code
/// matching) — so neither pair of handlers duplicates this logic.
/// </summary>
public sealed class RecoveryCodeService
{
    private readonly ITwoFactorRecoveryCodeRepository _repository;
    private readonly IRefreshTokenGenerator _codeGenerator;
    private readonly IRefreshTokenHasher _hasher;
    private readonly ITwoFactorPolicy _policy;

    public RecoveryCodeService(
        ITwoFactorRecoveryCodeRepository repository,
        IRefreshTokenGenerator codeGenerator,
        IRefreshTokenHasher hasher,
        ITwoFactorPolicy policy)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _hasher = hasher;
        _policy = policy;
    }

    /// <summary>Invalidates any previous batch and issues a fresh one, returned raw exactly once.</summary>
    public async Task<IReadOnlyCollection<string>> IssueNewBatchAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await _repository.DeleteAllForUserAsync(userId, cancellationToken);

        var rawCodes = new List<string>(_policy.RecoveryCodeCount);
        var entities = new List<TwoFactorRecoveryCode>(_policy.RecoveryCodeCount);

        for (var i = 0; i < _policy.RecoveryCodeCount; i++)
        {
            var raw = _codeGenerator.Generate();
            rawCodes.Add(raw);
            entities.Add(new TwoFactorRecoveryCode(userId, _hasher.Hash(raw), utcNow));
        }

        await _repository.AddRangeAsync(entities, cancellationToken);

        return rawCodes;
    }

    /// <summary>
    /// Finds and atomically consumes the recovery code matching the submitted
    /// value, if any. Returns true only if a still-valid code matched and this
    /// call won the race to consume it.
    /// </summary>
    public async Task<bool> TryConsumeMatchingAsync(Guid userId, string submittedCode, DateTime utcNow, CancellationToken cancellationToken)
    {
        var activeCodes = await _repository.GetActiveForUserAsync(userId, cancellationToken);
        var submittedHash = _hasher.Hash(submittedCode.Trim());
        var submittedBytes = Encoding.UTF8.GetBytes(submittedHash);

        var matchedCode = activeCodes.FirstOrDefault(
            code => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code.CodeHash), submittedBytes));

        if (matchedCode is null)
        {
            return false;
        }

        return await _repository.TryConsumeAsync(matchedCode.Id, utcNow, cancellationToken);
    }
}
