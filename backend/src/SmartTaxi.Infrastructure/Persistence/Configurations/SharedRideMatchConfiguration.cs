using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SharedRideMatchConfiguration : IEntityTypeConfiguration<SharedRideMatch>
{
    public void Configure(EntityTypeBuilder<SharedRideMatch> builder)
    {
        builder.ToTable("SharedRideMatches");

        builder.HasKey(match => match.Id);
        builder.Property(match => match.Id).ValueGeneratedNever();

        builder.Property(match => match.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(match => match.Status);

        builder.Property(match => match.DriverId);
        builder.Property(match => match.CreatedAt).IsRequired();
        builder.Property(match => match.ExpiresAt).IsRequired();
        builder.HasIndex(match => match.ExpiresAt);
        builder.Property(match => match.ConfirmedAt);
    }
}
