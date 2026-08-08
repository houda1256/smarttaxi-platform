namespace SmartTaxi.API.Contracts.Identity.Documents;

public sealed record RoleEligibilityResponse(string Role, bool IsEligible, IReadOnlyCollection<string> MissingOrInvalidTypes);
