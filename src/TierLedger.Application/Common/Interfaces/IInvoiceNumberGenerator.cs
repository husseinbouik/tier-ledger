namespace TierLedger.Application.Common.Interfaces;

/// <summary>
/// Hands out sequential, human-readable invoice numbers (INV-2026-0001).
/// Implemented in Infrastructure on top of a counter table so numbers stay
/// unique inside the same transaction that creates the invoice.
/// </summary>
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
