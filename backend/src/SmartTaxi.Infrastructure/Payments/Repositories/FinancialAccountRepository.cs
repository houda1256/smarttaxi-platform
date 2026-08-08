using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class FinancialAccountRepository : IFinancialAccountRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<FinancialAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        _context.FinancialAccounts.FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public Task<FinancialAccount?> GetByTypeAndOwnerAsync(FinancialAccountType accountType, Guid? ownerReferenceId, CancellationToken cancellationToken) =>
        _context.FinancialAccounts.FirstOrDefaultAsync(
            account => account.AccountType == accountType && account.OwnerReferenceId == ownerReferenceId, cancellationToken);

    public async Task<FinancialAccount> GetOrCreateAsync(FinancialAccountType accountType, Guid? ownerReferenceId, string currency, CancellationToken cancellationToken)
    {
        var existing = await GetByTypeAndOwnerAsync(accountType, ownerReferenceId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var account = FinancialAccount.Open(accountType, ownerReferenceId, currency, DateTime.UtcNow);
        await _context.FinancialAccounts.AddAsync(account, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException)
        {
            // Lost a concurrent first-use race against the unique (AccountType, OwnerReferenceId) index —
            // the other caller's account is now the real one.
            _context.Entry(account).State = EntityState.Detached;
            var raced = await GetByTypeAndOwnerAsync(accountType, ownerReferenceId, cancellationToken);
            return raced ?? throw new InvalidOperationException("Le compte financier concurrent est introuvable après l'échec de création.");
        }
    }

    public async Task<IReadOnlyCollection<FinancialAccount>> GetForOwnerReferenceAsync(Guid ownerReferenceId, CancellationToken cancellationToken) =>
        await _context.FinancialAccounts
            .Where(account => account.OwnerReferenceId == ownerReferenceId)
            .ToListAsync(cancellationToken);
}
