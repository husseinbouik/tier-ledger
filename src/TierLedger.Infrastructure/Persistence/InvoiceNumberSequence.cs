namespace TierLedger.Infrastructure.Persistence;

/// <summary>
/// Counter row (one per calendar year) backing the sequential invoice
/// numbering. The row is updated inside the same transaction that inserts the
/// invoice, and a unique index on the invoice number is the backstop against
/// duplicates under heavy concurrency.
/// </summary>
public sealed class InvoiceNumberSequence
{
    public int Year { get; set; }
    public int NextValue { get; set; }
}
