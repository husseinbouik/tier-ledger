using TierLedger.Domain.Common;

namespace TierLedger.Domain.Invoices;

/// <summary>
/// One invoice line. Amounts are computed, never entered:
/// net = quantity × unit price; tax = net × rate, rounded to 2 decimals
/// (midpoint away from zero, the usual accounting convention).
/// </summary>
public sealed class InvoiceLine
{
    private InvoiceLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    /// <summary>VAT rate as a percentage, e.g. 20 means 20%.</summary>
    public decimal TaxRatePercent { get; private set; }

    public decimal NetAmount => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    public decimal TaxAmount => Math.Round(NetAmount * TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal GrossAmount => NetAmount + TaxAmount;

    public static InvoiceLine Create(string description, decimal quantity, decimal unitPrice, decimal taxRatePercent)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Invoice line description is required.");
        if (quantity <= 0)
            throw new DomainException("Invoice line quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new DomainException("Invoice line unit price cannot be negative.");
        if (taxRatePercent is < 0 or > 100)
            throw new DomainException("Invoice line tax rate must be between 0 and 100 percent.");

        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRatePercent = taxRatePercent,
        };
    }
}
