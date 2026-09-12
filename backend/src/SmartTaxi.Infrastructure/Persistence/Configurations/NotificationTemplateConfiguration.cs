using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");

        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).ValueGeneratedNever();

        builder.Property(template => template.TemplateKey).HasMaxLength(200).IsRequired();
        builder.Property(template => template.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(template => template.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(template => template.Language).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(template => template.Subject).HasMaxLength(500);
        builder.Property(template => template.Body).HasMaxLength(4000).IsRequired();
        builder.Property(template => template.IsActive).IsRequired();
        builder.Property(template => template.Version).IsRequired();
        builder.Property(template => template.CreatedAtUtc).IsRequired();
        builder.Property(template => template.UpdatedAtUtc).IsRequired();

        builder.HasIndex(template => new { template.TemplateKey, template.Channel, template.Language }).IsUnique();
    }
}
