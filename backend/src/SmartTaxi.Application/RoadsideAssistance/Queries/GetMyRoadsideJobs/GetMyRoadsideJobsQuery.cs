using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideJobs;

public sealed record GetMyRoadsideJobsQuery(Guid PartnerUserId, int PageNumber, int PageSize) : IQuery<PagedResult<RoadsideAssistanceRequest>>;
