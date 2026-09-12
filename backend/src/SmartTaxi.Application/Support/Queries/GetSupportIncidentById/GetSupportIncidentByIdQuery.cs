using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetSupportIncidentById;

public sealed record GetSupportIncidentByIdQuery(Guid IncidentId) : IQuery<Result<SupportIncident>>;
