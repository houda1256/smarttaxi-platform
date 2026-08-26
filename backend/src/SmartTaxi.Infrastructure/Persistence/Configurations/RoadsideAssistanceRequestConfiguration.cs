using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RoadsideAssistanceRequestConfiguration : IEntityTypeConfiguration<RoadsideAssistanceRequest>
{
    public void Configure(EntityTypeBuilder<RoadsideAssistanceRequest> builder)
    {
        builder.ToTable("RoadsideAssistanceRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.RequesterUserId).IsRequired();
        builder.HasIndex(request => request.RequesterUserId);

        builder.Property(request => request.RequesterRole).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(request => request.VehicleId).IsRequired();
        builder.Property(request => request.RideId);

        builder.Property(request => request.ServiceType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(request => request.Urgency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.Description).HasMaxLength(2000).IsRequired();

        builder.Property(request => request.Latitude).IsRequired();
        builder.Property(request => request.Longitude).IsRequired();
        builder.Property(request => request.Address).HasMaxLength(500);
        builder.Property(request => request.City).HasMaxLength(100);

        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(request => request.Status);

        builder.Property(request => request.SelectedPartnerUserId);
        builder.HasIndex(request => request.SelectedPartnerUserId);

        builder.Property(request => request.EstimatedCost).HasPrecision(12, 3);
        builder.Property(request => request.FinalCost).HasPrecision(12, 3);

        builder.Property(request => request.RequestedAtUtc).IsRequired();
        builder.Property(request => request.AcceptedAtUtc);
        builder.Property(request => request.PartnerOnTheWayAtUtc);
        builder.Property(request => request.PartnerArrivedAtUtc);
        builder.Property(request => request.StartedAtUtc);
        builder.Property(request => request.CompletedAtUtc);
        builder.Property(request => request.CancelledAtUtc);
        builder.Property(request => request.ExpiredAtUtc);
        builder.Property(request => request.DisputedAtUtc);
        builder.Property(request => request.SettledAtUtc);

        builder.Property(request => request.CancellationReason).HasMaxLength(1000);
        builder.Property(request => request.CancelledByUserId);
        builder.Property(request => request.EscalatedMaintenanceRequestId);

        builder.Property(request => request.UpdatedAtUtc).IsRequired();

        // At most one non-terminal RoadsideAssistanceRequest per VehicleId — the real, race-safe
        // enforcement point for the "duplicate active request" constraint. Rejected is deliberately
        // NOT in the exclusion list (approved plan, Q1): a rejected cycle still occupies the vehicle's
        // one-active-request slot pending an explicit requester reselect/cancel/expire.
        builder.HasIndex(request => request.VehicleId)
            .IsUnique()
            .HasDatabaseName("IX_RoadsideAssistanceRequests_VehicleId_Active")
            .HasFilter("\"Status\" NOT IN ('Completed', 'Cancelled', 'Expired', 'Disputed')");

        builder.ToTable(t => t.HasCheckConstraint("CK_RoadsideAssistanceRequests_EstimatedCost_NonNegative", "\"EstimatedCost\" IS NULL OR \"EstimatedCost\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_RoadsideAssistanceRequests_FinalCost_NonNegative", "\"FinalCost\" IS NULL OR \"FinalCost\" >= 0"));
    }
}
