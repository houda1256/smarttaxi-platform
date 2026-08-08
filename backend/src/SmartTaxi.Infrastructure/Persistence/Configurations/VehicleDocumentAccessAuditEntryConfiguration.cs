using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class VehicleDocumentAccessAuditEntryConfiguration : IEntityTypeConfiguration<VehicleDocumentAccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<VehicleDocumentAccessAuditEntry> builder)
    {
        builder.ToTable("VehicleDocumentAccessAuditEntries");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.DocumentId).IsRequired();
        builder.HasIndex(entry => entry.DocumentId);

        builder.Property(entry => entry.AccessedByUserId).IsRequired();
        builder.Property(entry => entry.AccessType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.AccessedAt).IsRequired();
        builder.Property(entry => entry.IpAddress).HasMaxLength(45);
    }
}
