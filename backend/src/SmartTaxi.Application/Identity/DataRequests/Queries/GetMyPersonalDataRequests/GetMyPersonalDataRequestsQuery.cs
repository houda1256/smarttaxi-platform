using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataRequests;

public sealed record GetMyPersonalDataRequestsQuery(Guid UserId) : IQuery<IReadOnlyCollection<PersonalDataRequestSummary>>;
