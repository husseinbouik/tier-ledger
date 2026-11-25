using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.SubmitInvoice;

public sealed class SubmitInvoiceCommandHandler : IRequestHandler<SubmitInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public SubmitInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(SubmitInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.Submit();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
