using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Application.Payments.CashDeclarations.Queries.GetMyCashDeclarations;

public sealed record GetMyCashDeclarationsQuery(
    Guid DriverId, CashDeclarationStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<CashDeclaration>>;
