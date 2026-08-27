using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardQuery : IQuery<AdminDashboardSummary>;
