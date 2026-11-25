using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TierLedger.Domain.Invoices;

namespace TierLedger.Infrastructure.Persistence.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.TaxRatePercent).HasPrecision(5, 2);

        // Computed from quantity, price and rate; never stored.
        builder.Ignore(l => l.NetAmount);
        builder.Ignore(l => l.TaxAmount);
        builder.Ignore(l => l.GrossAmount);
    }
}
