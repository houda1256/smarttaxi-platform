using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FleetMemberConfiguration : IEntityTypeConfiguration<FleetMember>
{
    public void Configure(EntityTypeBuilder<FleetMember> builder)
    {
        builder.ToTable("FleetMembers");

        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id).ValueGeneratedNever();

        builder.Property(member => member.FleetId).IsRequired();
        builder.Property(member => member.UserId).IsRequired();
        builder.HasIndex(member => new { member.FleetId, member.UserId }).IsUnique();

        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(member => member.CreatedAt).IsRequired();
    }
}
