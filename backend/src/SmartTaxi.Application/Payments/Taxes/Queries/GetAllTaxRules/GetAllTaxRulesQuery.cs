using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Queries.GetAllTaxRules;

public sealed record GetAllTaxRulesQuery(int PageNumber, int PageSize) : IQuery<PagedResult<TaxRule>>;
