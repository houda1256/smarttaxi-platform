using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Domain.RoadsideAssistance.Entities;

/// <summary>
/// One row per manual-selection cycle — written by the same atomic call that
/// performs the SelectPartner transition (insert), and updated exactly once,
/// by the same atomic call that performs the partner's Accept/Reject
/// (guarded by Response IS NULL), same "plain entity written by the
/// transition itself" shape as RideStatusHistory. Once Response is set the
/// row is never modified again — a Rejected -> PartnersAvailable retry always
/// starts a NEW cycle with a NEW row rather than overwriting this one, so the
/// previous rejection is never silently erased (approved design decision).
/// Not an AggregateRoot — it has no independent lifecycle of its own, exactly
/// like RideStatusHistory.
/// </summary>
public sealed class RoadsidePartnerSelectionHistory
{
    public Guid Id { get; private set; }
    public Guid RoadsideAssistanceRequestId { get; private set; }
    public int CycleNumber { get; private set; }
    public Guid SelectedPartnerUserId { get; private set; }
    public DateTime SelectedAtUtc { get; private set; }
    public RoadsidePartnerResponse? Response { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }

    private RoadsidePartnerSelectionHistory()
    {
    }

    public RoadsidePartnerSelectionHistory(Guid roadsideAssistanceRequestId, int cycleNumber, Guid selectedPartnerUserId, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        RoadsideAssistanceRequestId = roadsideAssistanceRequestId;
        CycleNumber = cycleNumber;
        SelectedPartnerUserId = selectedPartnerUserId;
        SelectedAtUtc = utcNow;
    }
}
