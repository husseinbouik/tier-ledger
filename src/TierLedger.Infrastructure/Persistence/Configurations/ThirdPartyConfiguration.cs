using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Infrastructure.Persistence.Configurations;

public sealed class ThirdPartyConfiguration : IEntityTypeConfiguration<ThirdParty>
{
    public void Configure(EntityTypeBuilder<ThirdParty> builder)
    {
        builder.ToTable("ThirdParties");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.TaxId).IsRequired().HasMaxLength(32);
        builder.HasIndex(t => t.TaxId).IsUnique();
        builder.Property(t => t.Email).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Phone).HasMaxLength(32);
        builder.Property(t => t.IsActive).IsRequired();

        builder.OwnsOne(t => t.Address, address =>
        {
            address.Property(a => a.Street).IsRequired().HasMaxLength(200).HasColumnName("Address_Street");
            address.Property(a => a.City).IsRequired().HasMaxLength(100).HasColumnName("Address_City");
            address.Property(a => a.PostalCode).HasMaxLength(16).HasColumnName("Address_PostalCode");
            address.Property(a => a.Country).IsRequired().HasMaxLength(100).HasColumnName("Address_Country");
        });
        builder.Navigation(t => t.Address).IsRequired();
    }
}
