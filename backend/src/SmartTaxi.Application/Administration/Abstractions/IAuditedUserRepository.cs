using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Administration.Abstractions;

/// <summary>
/// The narrow seam AssignRoleCommandHandler/RemoveRoleCommandHandler use to
/// persist an already-mutated, already-validated User alongside its audit
/// entry in one transaction — a committed audit entry describing a role
/// change that rolled back (or vice versa) is unacceptable. This is
/// deliberately narrower than a general "save anything with an audit
/// entry" abstraction: it exists only for the one case Module 13A actually
/// needs, following the same "narrow cross-module seam" convention as every
/// other cross-cutting interface in this codebase.
/// </summary>
public interface IAuditedUserRepository
{
    Task SaveWithAuditAsync(User user, AuditLogEntry auditEntry, CancellationToken cancellationToken);
}
