using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.GetOutstandingBalance;

public sealed class GetOutstandingBalanceQueryHandler
    : IRequestHandler<GetOutstandingBalanceQuery, OutstandingBalanceDto>
{
    private readonly IApplicationDbContext _context;

    public GetOutstandingBalanceQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<OutstandingBalanceDto> Handle(
        GetOutstandingBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await _context.ThirdParties
            .AnyAsync(t => t.Id == request.ThirdPartyId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(ThirdParty), request.ThirdPartyId);

        var openInvoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Payments)
            .Where(i => i.ThirdPartyId == request.ThirdPartyId && i.Status == InvoiceStatus.Approved)
            .ToListAsync(cancellationToken);

        var balances = openInvoices
            .GroupBy(i => i.Currency)
            .Select(g => new CurrencyBalanceDto(g.Key, g.Sum(i => i.OutstandingAmount), g.Count()))
            .OrderBy(b => b.Currency)
            .ToList();

        return new OutstandingBalanceDto(request.ThirdPartyId, balances, openInvoices.Count);
    }
}
