using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideAssistanceRequests;

public sealed record GetMyRoadsideAssistanceRequestsQuery(Guid RequesterUserId, int PageNumber, int PageSize)
    : IQuery<PagedResult<RoadsideAssistanceRequest>>;
