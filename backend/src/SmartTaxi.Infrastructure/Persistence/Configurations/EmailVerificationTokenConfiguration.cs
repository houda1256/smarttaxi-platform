using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.UserId).IsRequired();
        builder.HasIndex(token => token.UserId);

        builder.Property(token => token.TokenHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
    }
}
