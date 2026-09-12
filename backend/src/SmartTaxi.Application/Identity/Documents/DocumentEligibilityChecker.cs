using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Documents.Policies;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Documents;

/// <summary>
/// Shared eligibility-check logic, reused by the read-only eligibility report
/// (for roles the user already holds) and by professional-account approval
/// (checking a role the user does NOT hold yet, before granting it).
/// </summary>
public sealed class DocumentEligibilityChecker
{
    private readonly IUserDocumentRepository _documentRepository;

    public DocumentEligibilityChecker(IUserDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<RoleEligibility> CheckAsync(Guid userId, UserRole role, DateTime utcNow, CancellationToken cancellationToken)
    {
        var criticalTypes = ProfessionalDocumentRequirements.GetCriticalDocumentTypes(role);
        var missing = new List<DocumentType>();

        foreach (var type in criticalTypes)
        {
            var latest = await _documentRepository.GetLatestForUserAndTypeAsync(userId, type, cancellationToken);

            if (latest is null || !latest.IsCurrentlyValid(utcNow))
            {
                missing.Add(type);
            }
        }

        return new RoleEligibility(role, missing.Count == 0, missing);
    }
}
