using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetRoadsideAssistanceRequestById;

public sealed record GetRoadsideAssistanceRequestByIdQuery(Guid RequestId, Guid RequestingUserId) : IQuery<Result<RoadsideAssistanceRequest>>;
