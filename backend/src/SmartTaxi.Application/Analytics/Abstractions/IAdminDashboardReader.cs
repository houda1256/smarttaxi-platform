using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Analytics.Abstractions;

public interface IAdminDashboardReader
{
    Task<AdminDashboardSummary> GetSnapshotAsync(CancellationToken cancellationToken);
}
