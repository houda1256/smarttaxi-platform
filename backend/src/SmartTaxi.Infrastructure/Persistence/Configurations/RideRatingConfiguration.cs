using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideRatingConfiguration : IEntityTypeConfiguration<RideRating>
{
    public void Configure(EntityTypeBuilder<RideRating> builder)
    {
        builder.ToTable("RideRatings");

        builder.HasKey(rating => rating.Id);
        builder.Property(rating => rating.Id).ValueGeneratedNever();

        builder.Property(rating => rating.RideId).IsRequired();
        builder.Property(rating => rating.ReviewerId).IsRequired();

        // One rating per reviewer per Ride — enforced by Postgres, not just the Application check.
        builder.HasIndex(rating => new { rating.RideId, rating.ReviewerId }).IsUnique();

        builder.Property(rating => rating.ReviewedUserId).IsRequired();
        builder.HasIndex(rating => rating.ReviewedUserId);

        builder.Property(rating => rating.Score).IsRequired();
        builder.Property(rating => rating.Comment).HasMaxLength(1000);
        builder.Property(rating => rating.Tags).HasMaxLength(500);
        builder.Property(rating => rating.IsReported).IsRequired();
        builder.Property(rating => rating.CreatedAt).IsRequired();
        builder.Property(rating => rating.UpdatedAt);
    }
}
