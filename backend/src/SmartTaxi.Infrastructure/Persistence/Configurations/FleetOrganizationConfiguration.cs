using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FleetOrganizationConfiguration : IEntityTypeConfiguration<FleetOrganization>
{
    public void Configure(EntityTypeBuilder<FleetOrganization> builder)
    {
        builder.ToTable("Fleets");

        builder.HasKey(fleet => fleet.Id);
        builder.Property(fleet => fleet.Id).ValueGeneratedNever();

        builder.Property(fleet => fleet.OwnerId).IsRequired();
        builder.HasIndex(fleet => fleet.OwnerId);

        builder.Property(fleet => fleet.Name).HasMaxLength(200).IsRequired();
        builder.Property(fleet => fleet.Description).HasMaxLength(1000);
        builder.Property(fleet => fleet.CityId).IsRequired();

        builder.Property(fleet => fleet.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(fleet => fleet.CreatedAt).IsRequired();
        builder.Property(fleet => fleet.UpdatedAt).IsRequired();
    }
}
