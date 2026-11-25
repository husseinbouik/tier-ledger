using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.CancelInvoice;

public sealed class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public CancelInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
