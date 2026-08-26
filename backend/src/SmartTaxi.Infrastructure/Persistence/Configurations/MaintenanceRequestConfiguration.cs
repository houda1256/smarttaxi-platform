using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("MaintenanceRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.VehicleId).IsRequired();

        builder.Property(request => request.OwnerUserId).IsRequired();
        builder.HasIndex(request => request.OwnerUserId);

        builder.Property(request => request.GarageUserId).IsRequired();
        builder.HasIndex(request => request.GarageUserId);

        builder.Property(request => request.Description).HasMaxLength(2000).IsRequired();

        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(request => request.Status);

        // At most one non-terminal MaintenanceRequest per VehicleId — the real, race-safe enforcement
        // point for the "duplicate active request" constraint (IMaintenanceRequestRepository.
        // HasActiveRequestForVehicleAsync is only a cheap pre-check for a friendlier validation error).
        builder.HasIndex(request => request.VehicleId)
            .IsUnique()
            .HasDatabaseName("IX_MaintenanceRequests_VehicleId_Active")
            .HasFilter("\"Status\" NOT IN ('Completed', 'Cancelled', 'Rejected', 'QuoteRejected')");

        builder.Property(request => request.RequestedAtUtc).IsRequired();
        builder.Property(request => request.ConfirmedAtUtc);
        builder.Property(request => request.GarageRejectionReason).HasMaxLength(1000);
        builder.Property(request => request.EstimatedCost).HasPrecision(12, 3);
        builder.Property(request => request.QuoteSubmittedAtUtc);
        builder.Property(request => request.QuoteAcceptedAtUtc);
        builder.Property(request => request.VehicleReceivedAtUtc);
        builder.Property(request => request.WorkStartedAtUtc);
        builder.Property(request => request.FinalCost).HasPrecision(12, 3);
        builder.Property(request => request.CompletedAtUtc);
        builder.Property(request => request.SettledAtUtc);
        builder.Property(request => request.CancelledAtUtc);
        builder.Property(request => request.CancellationReason).HasMaxLength(1000);
        builder.Property(request => request.CancelledByUserId);
        builder.Property(request => request.UpdatedAtUtc).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_MaintenanceRequests_EstimatedCost_NonNegative", "\"EstimatedCost\" IS NULL OR \"EstimatedCost\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_MaintenanceRequests_FinalCost_NonNegative", "\"FinalCost\" IS NULL OR \"FinalCost\" >= 0"));
    }
}
