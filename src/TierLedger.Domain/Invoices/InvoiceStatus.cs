namespace TierLedger.Domain.Invoices;

/// <summary>
/// Invoice lifecycle. Allowed transitions:
/// Draft → Submitted → Approved → Paid, with Submitted → Rejected → Draft
/// (revise) and Draft/Submitted → Cancelled as the escape hatches.
/// Paid is reached only through the payment process manager, once the invoice
/// is fully settled.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4,
    Cancelled = 5,
}
