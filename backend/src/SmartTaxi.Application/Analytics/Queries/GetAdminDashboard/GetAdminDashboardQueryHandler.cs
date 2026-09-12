using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler : IQueryHandler<GetAdminDashboardQuery, AdminDashboardSummary>
{
    private readonly IAdminDashboardReader _reader;

    public GetAdminDashboardQueryHandler(IAdminDashboardReader reader)
    {
        _reader = reader;
    }

    public Task<AdminDashboardSummary> Handle(GetAdminDashboardQuery query, CancellationToken cancellationToken) =>
        _reader.GetSnapshotAsync(cancellationToken);
}
