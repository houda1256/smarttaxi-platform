using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SharedRideParticipantConfiguration : IEntityTypeConfiguration<SharedRideParticipant>
{
    public void Configure(EntityTypeBuilder<SharedRideParticipant> builder)
    {
        builder.ToTable("SharedRideParticipants");

        builder.HasKey(participant => participant.Id);
        builder.Property(participant => participant.Id).ValueGeneratedNever();

        builder.Property(participant => participant.SharedRideMatchId).IsRequired();
        builder.HasIndex(participant => participant.SharedRideMatchId);

        builder.Property(participant => participant.RideId).IsRequired();
        builder.HasIndex(participant => participant.RideId);
        builder.HasIndex(participant => new { participant.SharedRideMatchId, participant.RideId }).IsUnique();

        builder.Property(participant => participant.CustomerId).IsRequired();
        builder.Property(participant => participant.ApprovalStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(participant => participant.CreatedAt).IsRequired();
        builder.Property(participant => participant.RespondedAt);
    }
}
