using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetAllSupportIncidents;

public sealed record GetAllSupportIncidentsQuery(int PageNumber, int PageSize) : IQuery<PagedResult<SupportIncident>>;
