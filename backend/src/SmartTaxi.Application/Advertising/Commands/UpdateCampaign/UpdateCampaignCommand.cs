using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.UpdateCampaign;

public sealed record UpdateCampaignCommand(
    Guid CampaignId, Guid RequestingUserId, string Name, string Description, string Objective, Guid PlacementId, DateTime StartAtUtc,
    DateTime EndAtUtc, AdPricingModel PricingModel, decimal PriceRate, decimal BudgetLimit, decimal? DailyBudgetLimit, string? TargetCity,
    string? TargetVehicleCategory, string? TargetDaysOfWeek, int? TargetStartHour, int? TargetEndHour) : ICommand<Result>;
