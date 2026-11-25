using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.ReturnInvoiceToDraft;

public sealed class ReturnInvoiceToDraftCommandHandler : IRequestHandler<ReturnInvoiceToDraftCommand>
{
    private readonly IApplicationDbContext _context;

    public ReturnInvoiceToDraftCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(ReturnInvoiceToDraftCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.ReturnToDraft();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
