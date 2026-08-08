using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideMessageConfiguration : IEntityTypeConfiguration<RideMessage>
{
    public void Configure(EntityTypeBuilder<RideMessage> builder)
    {
        builder.ToTable("RideMessages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.ConversationId).IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.SentAt });

        builder.Property(message => message.SenderId).IsRequired();
        builder.Property(message => message.MessageType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(message => message.Content).HasMaxLength(2000).IsRequired();
        builder.Property(message => message.IsReported).IsRequired();
        builder.Property(message => message.SentAt).IsRequired();
    }
}
