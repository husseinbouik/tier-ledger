using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TierLedger.Domain.Processes;

namespace TierLedger.Infrastructure.Persistence.Configurations;

public sealed class PaymentProcessConfiguration : IEntityTypeConfiguration<PaymentProcess>
{
    public void Configure(EntityTypeBuilder<PaymentProcess> builder)
    {
        builder.ToTable("PaymentProcesses");
        builder.HasKey(p => p.Id);

        // Exactly one payment process per invoice.
        builder.HasIndex(p => p.InvoiceId).IsUnique();

        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TotalDue).HasPrecision(18, 2);
        builder.Property(p => p.AmountPaid).HasPrecision(18, 2);

        builder.Ignore(p => p.OutstandingAmount);
        builder.Ignore(p => p.IsFullyPaid);
    }
}
