using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideFareProposalConfiguration : IEntityTypeConfiguration<RideFareProposal>
{
    public void Configure(EntityTypeBuilder<RideFareProposal> builder)
    {
        builder.ToTable("RideFareProposals");

        builder.HasKey(proposal => proposal.Id);
        builder.Property(proposal => proposal.Id).ValueGeneratedNever();

        builder.Property(proposal => proposal.RideId).IsRequired();
        builder.HasIndex(proposal => proposal.RideId);
        builder.HasIndex(proposal => new { proposal.RideId, proposal.RoundNumber });

        builder.Property(proposal => proposal.ProposedBy).IsRequired();
        builder.Property(proposal => proposal.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(proposal => proposal.Currency).HasMaxLength(3).IsRequired();
        builder.Property(proposal => proposal.RoundNumber).IsRequired();
        builder.Property(proposal => proposal.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(proposal => proposal.CreatedAt).IsRequired();
        builder.Property(proposal => proposal.ExpiresAt).IsRequired();
    }
}
