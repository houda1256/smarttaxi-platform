using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Commands.RequestPayout;

/// <summary>BeneficiaryOwnerReferenceId is the beneficiary's UserId (Driver/TaxiOwner/GaragePartner/RoadsideAssistancePartner), matching FinancialAccount's own OwnerReferenceId convention.</summary>
public sealed record RequestPayoutCommand(
    Guid RequestedByUserId, FinancialAccountType BeneficiaryType, Guid BeneficiaryOwnerReferenceId, decimal Amount,
    string Currency, PayoutMethod Method, PayoutFrequency Frequency) : ICommand<Result<Guid>>;
