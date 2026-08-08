using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Ledger.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFinancialLedgerRepository : IFinancialLedgerRepository
{
    private readonly List<FinancialLedgerEntry> _entries = [];
    private readonly FakeFinancialAccountRepository _accountRepository;

    public FakeFinancialLedgerRepository(FakeFinancialAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public IReadOnlyCollection<FinancialLedgerEntry> AllEntries => _entries;

    public async Task<bool> PostBatchAsync(
        string sourceType, Guid sourceId, string currency, Guid? createdBy, DateTime utcNow,
        IReadOnlyCollection<LedgerPostingLine> lines, CancellationToken cancellationToken)
    {
        if (_entries.Any(e => e.SourceType == sourceType && e.SourceId == sourceId))
        {
            return false;
        }

        foreach (var line in lines)
        {
            var entry = FinancialLedgerEntry.Post(
                line.DebitAccountId, line.CreditAccountId, line.Amount, currency, line.EntryType, sourceType, sourceId,
                line.Description, createdBy, utcNow);
            _entries.Add(entry);

            var debitAccount = await _accountRepository.GetByIdAsync(line.DebitAccountId, cancellationToken);
            var creditAccount = await _accountRepository.GetByIdAsync(line.CreditAccountId, cancellationToken);

            foreach (var effect in line.DebitEffects)
            {
                ApplyEffect(debitAccount!.Id, effect);
            }

            foreach (var effect in line.CreditEffects)
            {
                ApplyEffect(creditAccount!.Id, effect);
            }
        }

        return true;
    }

    public Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FinancialLedgerEntry> entries = _entries.Where(e => e.SourceType == sourceType && e.SourceId == sourceId).ToList();
        return Task.FromResult(entries);
    }

    public Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForAccountAsync(
        Guid accountId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = _entries.Where(e => e.DebitAccountId == accountId || e.CreditAccountId == accountId);

        if (fromUtc is not null)
        {
            query = query.Where(e => e.CreatedAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(e => e.CreatedAt <= toUtc);
        }

        IReadOnlyCollection<FinancialLedgerEntry> entries = query.ToList();
        return Task.FromResult(entries);
    }

    public Task<bool> HasEntriesForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.Any(e => e.SourceType == sourceType && e.SourceId == sourceId));

    private void ApplyEffect(Guid accountId, FinancialBalanceEffect effect)
    {
        var propertyName = effect.Bucket switch
        {
            FinancialBalanceBucket.Pending => "PendingBalance",
            FinancialBalanceBucket.Available => "AvailableBalance",
            FinancialBalanceBucket.Reserved => "ReservedBalance",
            FinancialBalanceBucket.PaidOut => "PaidOutBalance",
            FinancialBalanceBucket.Debt => "DebtBalance",
            _ => throw new ArgumentOutOfRangeException(nameof(effect))
        };

        var account = _accountRepository.GetByIdAsync(accountId, CancellationToken.None).GetAwaiter().GetResult();
        var currentValue = (decimal)typeof(Domain.Payments.Accounts.Entities.FinancialAccount).GetProperty(propertyName)!.GetValue(account)!;
        _accountRepository.SetProperty(accountId, propertyName, currentValue + effect.Delta);
    }
}
