using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<Result<GetUserByIdResult>>;
