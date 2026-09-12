using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetMyProfessionalAccountRequests;

public sealed record GetMyProfessionalAccountRequestsQuery(Guid UserId)
    : IQuery<IReadOnlyCollection<ProfessionalAccountRequestSummary>>;
