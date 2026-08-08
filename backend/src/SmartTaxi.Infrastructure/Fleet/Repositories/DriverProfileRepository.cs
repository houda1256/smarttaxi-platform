using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class DriverProfileRepository : IDriverProfileRepository
{
    private readonly ApplicationDbContext _context;

    public DriverProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DriverProfile profile, CancellationToken cancellationToken)
    {
        await _context.DriverProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<DriverProfile?> GetByIdAsync(Guid driverProfileId, CancellationToken cancellationToken) =>
        _context.DriverProfiles.FirstOrDefaultAsync(profile => profile.Id == driverProfileId, cancellationToken);

    public Task<DriverProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.DriverProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task<IReadOnlyCollection<DriverProfile>> GetEligibleAsync(CancellationToken cancellationToken) =>
        await _context.DriverProfiles
            .Where(profile => profile.VerificationStatus == DriverVerificationStatus.Approved
                && profile.AvailabilityStatus == DriverAvailabilityStatus.Available)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(DriverProfile profile, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySubmitForReviewAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverProfiles
            .Where(profile => profile.Id == driverProfileId && profile.VerificationStatus == DriverVerificationStatus.PendingReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(profile => profile.VerificationStatus, DriverVerificationStatus.UnderReview)
                .SetProperty(profile => profile.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryApproveAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverProfiles
            .Where(profile => profile.Id == driverProfileId && profile.VerificationStatus == DriverVerificationStatus.UnderReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(profile => profile.VerificationStatus, DriverVerificationStatus.Approved)
                .SetProperty(profile => profile.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRejectAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverProfiles
            .Where(profile => profile.Id == driverProfileId && profile.VerificationStatus == DriverVerificationStatus.UnderReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(profile => profile.VerificationStatus, DriverVerificationStatus.Rejected)
                .SetProperty(profile => profile.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySuspendAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverProfiles
            .Where(profile => profile.Id == driverProfileId && profile.VerificationStatus == DriverVerificationStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(profile => profile.VerificationStatus, DriverVerificationStatus.Suspended)
                .SetProperty(profile => profile.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
