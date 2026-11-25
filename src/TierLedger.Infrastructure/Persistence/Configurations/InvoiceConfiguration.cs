using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number).IsRequired().HasMaxLength(20);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.Property(i => i.RejectionReason).HasMaxLength(500);
        builder.Property(i => i.NetAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.GrossAmount).HasPrecision(18, 2);

        // Computed from the payments collection; never stored.
        builder.Ignore(i => i.TotalPaid);
        builder.Ignore(i => i.OutstandingAmount);

        builder.HasOne<ThirdParty>()
            .WithMany()
            .HasForeignKey(i => i.ThirdPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(i => i.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
