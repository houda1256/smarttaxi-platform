using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationById;

public sealed record GetCashDeclarationByIdQuery(Guid CashDeclarationId) : IQuery<Result<CashDeclaration>>;
