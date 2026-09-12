using SmartTaxi.Application.RoadsideAssistance.Abstractions;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Policies;

/// <summary>
/// Fixed default (not yet DB/appsettings-configurable in this sub-slice,
/// same documented interim limitation as ProfessionalDocumentRequirements) —
/// a request sitting in PartnersAvailable/PendingPartnerResponse/Rejected for
/// more than 2 hours is eligible for the manual expire-sweep.
/// </summary>
internal sealed class RoadsideExpiryPolicy : IRoadsideExpiryPolicy
{
    public TimeSpan StaleAfter => TimeSpan.FromHours(2);
}
