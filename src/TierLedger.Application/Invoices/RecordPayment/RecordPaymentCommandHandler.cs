using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.RecordPayment;

public sealed class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public RecordPaymentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        // Payments are included: the aggregate guards against overpayment
        // based on what has already been paid.
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var payment = invoice.RegisterPayment(
            request.Amount,
            request.Method,
            request.Reference,
            request.PaidOnUtc ?? DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
