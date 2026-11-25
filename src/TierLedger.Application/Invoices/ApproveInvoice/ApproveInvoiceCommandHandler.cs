using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.ApproveInvoice;

public sealed class ApproveInvoiceCommandHandler : IRequestHandler<ApproveInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public ApproveInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.Approve();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
