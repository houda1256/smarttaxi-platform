using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetPendingProfessionalAccountRequests;

public sealed record GetPendingProfessionalAccountRequestsQuery
    : IQuery<IReadOnlyCollection<ProfessionalAccountRequestSummary>>;
