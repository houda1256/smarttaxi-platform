using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideConversationConfiguration : IEntityTypeConfiguration<RideConversation>
{
    public void Configure(EntityTypeBuilder<RideConversation> builder)
    {
        builder.ToTable("RideConversations");

        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Id).ValueGeneratedNever();

        builder.Property(conversation => conversation.RideId).IsRequired();
        builder.HasIndex(conversation => conversation.RideId).IsUnique();

        builder.Property(conversation => conversation.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(conversation => conversation.CreatedAt).IsRequired();
        builder.Property(conversation => conversation.ReadOnlyAt);
    }
}
