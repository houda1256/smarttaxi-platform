using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetPendingPersonalDataRequestsAdmin;

public sealed record GetPendingPersonalDataRequestsAdminQuery : IQuery<IReadOnlyCollection<PersonalDataRequestSummary>>;
