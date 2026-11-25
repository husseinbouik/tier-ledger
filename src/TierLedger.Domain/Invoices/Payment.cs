namespace TierLedger.Domain.Invoices;

/// <summary>A single payment recorded against an approved invoice. Payments may be partial.</summary>
public sealed class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public DateTime PaidOnUtc { get; private set; }
    public DateTime RecordedOnUtc { get; private set; }

    internal static Payment Create(Guid invoiceId, decimal amount, PaymentMethod method, string? reference, DateTime paidOnUtc)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Amount = amount,
            Method = method,
            Reference = reference?.Trim(),
            PaidOnUtc = paidOnUtc,
            RecordedOnUtc = DateTime.UtcNow,
        };
    }
}
