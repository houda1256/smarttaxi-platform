using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyLoyaltySummary;

/// <summary>No account yet is a valid, normal state (the user has not earned anything) — returns null, not an error.</summary>
public sealed class GetMyLoyaltySummaryQueryHandler : IQueryHandler<GetMyLoyaltySummaryQuery, LoyaltyAccountSummary?>
{
    private readonly ILoyaltyAccountRepository _accountRepository;

    public GetMyLoyaltySummaryQueryHandler(ILoyaltyAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<LoyaltyAccountSummary?> Handle(GetMyLoyaltySummaryQuery query, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return account is null ? null : LoyaltyAccountSummary.FromEntity(account);
    }
}
