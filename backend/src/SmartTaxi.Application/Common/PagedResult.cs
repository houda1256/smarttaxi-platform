namespace SmartTaxi.Application.Common;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize);
