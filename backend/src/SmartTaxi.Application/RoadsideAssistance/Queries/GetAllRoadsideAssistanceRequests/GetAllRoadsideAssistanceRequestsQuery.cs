using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetAllRoadsideAssistanceRequests;

public sealed record GetAllRoadsideAssistanceRequestsQuery(int PageNumber, int PageSize) : IQuery<PagedResult<RoadsideAssistanceRequest>>;
