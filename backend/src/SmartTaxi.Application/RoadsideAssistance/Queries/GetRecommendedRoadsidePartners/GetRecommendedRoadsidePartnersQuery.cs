using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Contracts;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetRecommendedRoadsidePartners;

public sealed record GetRecommendedRoadsidePartnersQuery(Guid RequestId, Guid RequestingUserId)
    : IQuery<Result<IReadOnlyCollection<RecommendedRoadsidePartner>>>;
