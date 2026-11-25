using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.RejectInvoice;

public sealed class RejectInvoiceCommandHandler : IRequestHandler<RejectInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public RejectInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(RejectInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.Reject(request.Reason);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
