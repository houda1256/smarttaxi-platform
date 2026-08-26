using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsidePartnerProfile;

public sealed record GetMyRoadsidePartnerProfileQuery(Guid UserId) : IQuery<RoadsidePartnerProfile?>;
