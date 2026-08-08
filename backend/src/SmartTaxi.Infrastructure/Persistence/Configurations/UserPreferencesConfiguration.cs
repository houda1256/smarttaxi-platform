using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Preferences.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(preferences => preferences.UserId);

        builder.Property(preferences => preferences.Language).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(preferences => preferences.NotificationChannels).HasConversion<string>().HasMaxLength(100).IsRequired();

        builder.Property(preferences => preferences.Timezone).HasMaxLength(100).IsRequired();
        builder.Property(preferences => preferences.DisplayName).HasMaxLength(200);
        builder.Property(preferences => preferences.AvatarUrl).HasMaxLength(1000);

        builder.Property(preferences => preferences.CreatedAt).IsRequired();
        builder.Property(preferences => preferences.UpdatedAt).IsRequired();
    }
}
