using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("SupportTicketMessages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.TicketId).IsRequired();
        builder.HasIndex(message => message.TicketId);

        builder.Property(message => message.AuthorUserId).IsRequired();
        builder.Property(message => message.Body).HasMaxLength(4000).IsRequired();
        builder.Property(message => message.IsInternalNote).IsRequired();
        builder.Property(message => message.CreatedAtUtc).IsRequired();
    }
}
