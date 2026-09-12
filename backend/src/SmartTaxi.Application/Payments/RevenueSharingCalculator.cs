using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Payments;

/// <summary>
/// Lives directly in Application (like Ride's DriverRecommendationService)
/// because it composes Fleet's Application-layer contract repository — not a
/// swappable infrastructure concern. Deterministic and fully explainable:
/// every split traces back to the platform commission percentage and, when
/// one exists, the Driver-Owner contract's own terms. Never payroll: a
/// Driver's periodic salary/rental payments are settled entirely outside
/// this per-ride distribution.
/// </summary>
public sealed class RevenueSharingCalculator : IRevenueSharingCalculator
{
    private readonly IDriverOwnerContractRepository _contractRepository;
    private readonly IPlatformCommissionPolicy _commissionPolicy;

    public RevenueSharingCalculator(IDriverOwnerContractRepository contractRepository, IPlatformCommissionPolicy commissionPolicy)
    {
        _contractRepository = contractRepository;
        _commissionPolicy = commissionPolicy;
    }

    public async Task<RevenueShareResult> CalculateAsync(Guid driverId, Guid ownerId, decimal fareAmount, CancellationToken cancellationToken)
    {
        var platformCommission = Math.Round(fareAmount * _commissionPolicy.CommissionPercentage / 100m, 2);
        var remainder = fareAmount - platformCommission;

        if (driverId == ownerId)
        {
            return new RevenueShareResult(
                remainder, 0m, platformCommission,
                $"Chauffeur indépendant : commission plateforme {_commissionPolicy.CommissionPercentage}%, le reste revient entièrement au chauffeur (propriétaire de son propre véhicule).");
        }

        var contract = await _contractRepository.GetActiveForDriverAndOwnerAsync(driverId, ownerId, cancellationToken);

        if (contract is null)
        {
            return new RevenueShareResult(
                remainder, 0m, platformCommission,
                $"Commission plateforme {_commissionPolicy.CommissionPercentage}% ; aucun contrat actif trouvé — le reste est attribué par défaut au chauffeur en attendant régularisation.");
        }

        if (contract.ContractType == ContractType.PercentagePerRide)
        {
            var driverAmount = Math.Round(remainder * contract.DriverPercentage!.Value / 100m, 2);
            var ownerAmount = remainder - driverAmount;

            return new RevenueShareResult(
                driverAmount, ownerAmount, platformCommission,
                $"Commission plateforme {_commissionPolicy.CommissionPercentage}% ; contrat au pourcentage : {contract.DriverPercentage}% chauffeur / {contract.OwnerPercentage}% propriétaire.");
        }

        // FixedSalary/DailyRental/WeeklyRental/MonthlyRental: the Driver's periodic pay is
        // settled outside any single ride, so the full per-ride remainder is credited to
        // the Owner's ledger. CustomAgreement has no machine-readable split, so it falls
        // back to the same safe default pending manual reconciliation.
        return new RevenueShareResult(
            0m, remainder, platformCommission,
            $"Commission plateforme {_commissionPolicy.CommissionPercentage}% ; contrat de type {contract.ContractType} — le reste est crédité au propriétaire (rémunération du chauffeur réglée hors répartition par course).");
    }
}
