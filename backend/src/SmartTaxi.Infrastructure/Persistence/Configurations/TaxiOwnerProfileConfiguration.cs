using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Owners.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class TaxiOwnerProfileConfiguration : IEntityTypeConfiguration<TaxiOwnerProfile>
{
    public void Configure(EntityTypeBuilder<TaxiOwnerProfile> builder)
    {
        builder.ToTable("TaxiOwnerProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.OwnerType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(profile => profile.FirstName).HasMaxLength(100);
        builder.Property(profile => profile.LastName).HasMaxLength(100);
        builder.Property(profile => profile.NationalId).HasMaxLength(50);

        builder.Property(profile => profile.LegalName).HasMaxLength(200);
        builder.Property(profile => profile.TradeName).HasMaxLength(200);
        builder.Property(profile => profile.TaxIdentifier).HasMaxLength(50);
        builder.Property(profile => profile.RegistrationNumber).HasMaxLength(50);

        // OwnsOne is safe here (unlike User.Email, see UserConfiguration): TaxiOwnerProfile
        // is always materialized via its parameterless constructor (its parameterized ctor's
        // "utcNow" parameter has no matching property, so EF can't select it), so Address and
        // BankInformation are never required as constructor arguments of the owner.
        builder.OwnsOne(profile => profile.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(200).IsRequired();
            address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(100).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("AddressCountry").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(profile => profile.BankInformation, bank =>
        {
            bank.Property(b => b.BankName).HasColumnName("BankName").HasMaxLength(200).IsRequired();
            bank.Property(b => b.AccountHolderName).HasColumnName("BankAccountHolderName").HasMaxLength(200).IsRequired();
            bank.Property(b => b.AccountNumber).HasColumnName("BankAccountNumber").HasMaxLength(50).IsRequired();
            bank.Property(b => b.SwiftOrBic).HasColumnName("BankSwiftOrBic").HasMaxLength(20);
        });

        builder.Property(profile => profile.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(profile => profile.AccountStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(profile => profile.CreatedAt).IsRequired();
        builder.Property(profile => profile.UpdatedAt).IsRequired();
    }
}
