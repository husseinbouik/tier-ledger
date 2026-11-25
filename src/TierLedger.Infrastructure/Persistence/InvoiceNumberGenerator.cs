using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;

namespace TierLedger.Infrastructure.Persistence;

/// <summary>
/// Sequential invoice numbers per calendar year (INV-2026-0001, INV-2026-0002…).
/// The counter row lives in the same database and is written by the caller's
/// SaveChanges, so the number reservation and the invoice insert commit
/// together. A unique index on Invoices.Number is the backstop: if two
/// concurrent transactions race for the same value, one fails loudly instead
/// of issuing a duplicate number.
/// </summary>
public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly AppDbContext _context;

    public InvoiceNumberGenerator(AppDbContext context) => _context = context;

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;

        var sequence = await _context.InvoiceNumberSequences
            .FirstOrDefaultAsync(s => s.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new InvoiceNumberSequence { Year = year, NextValue = 1 };
            _context.InvoiceNumberSequences.Add(sequence);
        }

        var value = sequence.NextValue;
        sequence.NextValue++;

        return $"INV-{year}-{value:D4}";
    }
}
