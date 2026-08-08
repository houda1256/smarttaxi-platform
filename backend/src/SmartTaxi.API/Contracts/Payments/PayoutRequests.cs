using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record RequestPayoutRequest(
    FinancialAccountType BeneficiaryType, decimal Amount, string Currency, PayoutMethod Method, PayoutFrequency Frequency);

public sealed record FailPayoutRequest(string Reason);
