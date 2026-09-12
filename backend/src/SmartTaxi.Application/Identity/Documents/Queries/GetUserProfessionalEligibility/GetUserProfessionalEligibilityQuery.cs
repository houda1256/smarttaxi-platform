using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetUserProfessionalEligibility;

public sealed record GetUserProfessionalEligibilityQuery(Guid UserId)
    : IQuery<Result<IReadOnlyCollection<RoleEligibility>>>;
