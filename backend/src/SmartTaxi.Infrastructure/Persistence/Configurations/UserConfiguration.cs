using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        // Value converters (not OwnsOne) so EF Core's constructor binding for User's
        // private constructor can bind Email/PasswordHash as scalar properties —
        // owned-type navigations cannot be bound to constructor parameters.
        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasConversion(hash => hash.Value, value => HashedPassword.Create(value))
            .HasColumnName("PasswordHash")
            .IsRequired();

        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.EmailVerifiedAt);
        builder.Property(u => u.PhoneVerifiedAt);

        builder.Property(u => u.TwoFactorEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.TwoFactorActiveSecretEncrypted);
        builder.Property(u => u.TwoFactorPendingSecretEncrypted);
        builder.Property(u => u.TwoFactorPendingSecretCreatedAt);
        builder.Property(u => u.TwoFactorConfirmedAt);

        builder.Property(u => u.ReferralCode).HasMaxLength(20);
        builder.HasIndex(u => u.ReferralCode).IsUnique();

        // Roles are persisted in a separate table via the private _roleAssignments
        // backing field. User only exposes a read-only Roles projection plus
        // AssignRole/RemoveRole behavior — never a settable collection — so this
        // navigation is configured against the field directly (no property exists).
        builder.OwnsMany<UserRoleAssignment>("_roleAssignments", roles =>
        {
            roles.ToTable("UserRoles");
            roles.WithOwner().HasForeignKey(r => r.UserId);
            roles.Property(r => r.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
            roles.HasKey(r => new { r.UserId, r.Role });
        });

        builder.Navigation("_roleAssignments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
