namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>
/// Admin escape hatch, allowed from any non-terminal status. ONE transaction:
/// transitions RoadsideAssistanceRequest to Cancelled (guarded on current
/// status only — no partner/requester requirement, this is an admin action),
/// then — ONLY when the request's ServiceType requires vehicle immobilization
/// AND the current status is InProgress (the only case Fleet could actually
/// have been touched) — attempts a best-effort, non-gating Fleet release,
/// same non-gating semantics as IRoadsideCompletionRepository. Non-immobilizing
/// service types and any pre-InProgress status never call Fleet at all — the
/// vehicle was never touched in the first place.
/// </summary>
public interface IRoadsideForceCancelRepository
{
    Task<bool> TryForceCancelAsync(Guid requestId, Guid adminUserId, string reason, DateTime utcNow, CancellationToken cancellationToken);
}
