using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Common;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.Invoices.CreateInvoice;

public sealed class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IInvoiceNumberGenerator _numberGenerator;

    public CreateInvoiceCommandHandler(IApplicationDbContext context, IInvoiceNumberGenerator numberGenerator)
    {
        _context = context;
        _numberGenerator = numberGenerator;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var thirdParty = await _context.ThirdParties
            .FirstOrDefaultAsync(t => t.Id == request.ThirdPartyId, cancellationToken)
            ?? throw new NotFoundException(nameof(ThirdParty), request.ThirdPartyId);

        if (!thirdParty.IsActive)
            throw new DomainException(
                $"Cannot create an invoice for deactivated third party '{thirdParty.Name}'.");

        var number = await _numberGenerator.NextAsync(cancellationToken);

        var lines = request.Lines
            .Select(l => InvoiceLine.Create(l.Description, l.Quantity, l.UnitPrice, l.TaxRatePercent))
            .ToList();

        var invoice = Invoice.Create(
            number,
            request.ThirdPartyId,
            request.IssueDate,
            request.DueDate,
            request.Currency,
            request.Notes,
            lines);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
