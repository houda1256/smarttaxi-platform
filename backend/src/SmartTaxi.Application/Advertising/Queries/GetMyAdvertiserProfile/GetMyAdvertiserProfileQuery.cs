using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetMyAdvertiserProfile;

public sealed record GetMyAdvertiserProfileQuery(Guid UserId) : IQuery<AdvertiserProfile?>;
