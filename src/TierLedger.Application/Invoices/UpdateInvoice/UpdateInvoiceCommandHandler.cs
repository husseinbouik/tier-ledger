using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.UpdateInvoice;

public sealed class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Lines are included so EF Core tracks the replaced line set correctly.
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        var lines = request.Lines
            .Select(l => InvoiceLine.Create(l.Description, l.Quantity, l.UnitPrice, l.TaxRatePercent))
            .ToList();

        invoice.Revise(request.DueDate, request.Currency, request.Notes, lines);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
