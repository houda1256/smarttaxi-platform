using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideDriverRecommendationConfiguration : IEntityTypeConfiguration<RideDriverRecommendation>
{
    public void Configure(EntityTypeBuilder<RideDriverRecommendation> builder)
    {
        builder.ToTable("RideDriverRecommendations");

        builder.HasKey(recommendation => recommendation.Id);
        builder.Property(recommendation => recommendation.Id).ValueGeneratedNever();

        builder.Property(recommendation => recommendation.RideId).IsRequired();
        builder.HasIndex(recommendation => recommendation.RideId);

        builder.Property(recommendation => recommendation.DriverId).IsRequired();
        builder.Property(recommendation => recommendation.VehicleId).IsRequired();

        builder.Property(recommendation => recommendation.DistanceToPickupKm).HasPrecision(8, 2).IsRequired();
        builder.Property(recommendation => recommendation.EstimatedArrivalMinutes).IsRequired();
        builder.Property(recommendation => recommendation.RecommendationScore).HasPrecision(8, 2).IsRequired();
        builder.Property(recommendation => recommendation.RecommendationReasons).HasMaxLength(1000).IsRequired();
        builder.Property(recommendation => recommendation.Rank).IsRequired();
        builder.Property(recommendation => recommendation.CreatedAt).IsRequired();
    }
}
