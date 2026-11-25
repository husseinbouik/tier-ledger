using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TierLedger.Infrastructure.Persistence.Configurations;

public sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> builder)
    {
        builder.ToTable("InvoiceNumberSequences");
        builder.HasKey(s => s.Year);
        builder.Property(s => s.Year).ValueGeneratedNever();
        builder.Property(s => s.NextValue).IsRequired();
    }
}
