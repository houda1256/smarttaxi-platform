using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Policies;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetUserProfessionalEligibility;

/// <summary>
/// Read-only eligibility report. No automatic enforcement action is taken
/// here — there is no Rides/Fleet consumer yet to restrict anything against;
/// this query is the integration point future modules will call.
/// </summary>
public sealed class GetUserProfessionalEligibilityQueryHandler
    : IQueryHandler<GetUserProfessionalEligibilityQuery, Result<IReadOnlyCollection<RoleEligibility>>>
{
    private const string NotFoundError = "Utilisateur introuvable.";

    private readonly IUserRepository _userRepository;
    private readonly DocumentEligibilityChecker _eligibilityChecker;

    public GetUserProfessionalEligibilityQueryHandler(
        IUserRepository userRepository, DocumentEligibilityChecker eligibilityChecker)
    {
        _userRepository = userRepository;
        _eligibilityChecker = eligibilityChecker;
    }

    public async Task<Result<IReadOnlyCollection<RoleEligibility>>> Handle(
        GetUserProfessionalEligibilityQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
        {
            return Result<IReadOnlyCollection<RoleEligibility>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var report = new List<RoleEligibility>();

        foreach (var role in user.Roles)
        {
            if (ProfessionalDocumentRequirements.GetCriticalDocumentTypes(role).Count == 0)
            {
                continue;
            }

            report.Add(await _eligibilityChecker.CheckAsync(user.Id, role, utcNow, cancellationToken));
        }

        return Result<IReadOnlyCollection<RoleEligibility>>.Success(report);
    }
}
