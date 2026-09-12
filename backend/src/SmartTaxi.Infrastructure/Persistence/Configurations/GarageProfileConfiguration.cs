using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class GarageProfileConfiguration : IEntityTypeConfiguration<GarageProfile>
{
    public void Configure(EntityTypeBuilder<GarageProfile> builder)
    {
        builder.ToTable("GarageProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.LegalName).HasMaxLength(200);
        builder.Property(profile => profile.Address).HasMaxLength(500).IsRequired();
        builder.Property(profile => profile.City).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.SupportedVehicleCategories).HasMaxLength(200);
        builder.Property(profile => profile.AvailableServices).HasMaxLength(1000);

        builder.Property(profile => profile.IsActive).IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc).IsRequired();
    }
}
