using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetAdminPayouts;

public sealed record GetAdminPayoutsQuery(
    FinancialAccountType? BeneficiaryType, PayoutStatus? Status, DateTime? FromUtc, DateTime? ToUtc,
    int PageNumber, int PageSize) : IQuery<PagedResult<Payout>>;
