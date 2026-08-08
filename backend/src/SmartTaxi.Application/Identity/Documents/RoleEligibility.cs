using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Documents;

public sealed record RoleEligibility(UserRole Role, bool IsEligible, IReadOnlyCollection<DocumentType> MissingOrInvalidTypes);
