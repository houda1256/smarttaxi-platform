using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RoadsidePartnerProfileConfiguration : IEntityTypeConfiguration<RoadsidePartnerProfile>
{
    public void Configure(EntityTypeBuilder<RoadsidePartnerProfile> builder)
    {
        builder.ToTable("RoadsidePartnerProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.LegalName).HasMaxLength(200);
        builder.Property(profile => profile.Address).HasMaxLength(500).IsRequired();
        builder.Property(profile => profile.City).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.SupportedServiceTypes).HasMaxLength(500);
        builder.Property(profile => profile.SupportedVehicleCategories).HasMaxLength(200);
        builder.Property(profile => profile.Latitude);
        builder.Property(profile => profile.Longitude);

        builder.Property(profile => profile.IsActive).IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc).IsRequired();
    }
}
