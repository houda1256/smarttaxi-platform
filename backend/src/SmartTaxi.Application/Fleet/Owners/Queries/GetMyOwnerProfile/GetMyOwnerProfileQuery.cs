using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Owners;

namespace SmartTaxi.Application.Fleet.Owners.Queries.GetMyOwnerProfile;

public sealed record GetMyOwnerProfileQuery(Guid UserId) : IQuery<Result<OwnerProfileSummary>>;
