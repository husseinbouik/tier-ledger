using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Application.Common.Models;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.ListInvoices;

public sealed class ListInvoicesQueryHandler : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public ListInvoicesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedResult<InvoiceListItemDto>> Handle(
        ListInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Payments)
            .AsQueryable();

        if (request.ThirdPartyId is not null)
            query = query.Where(i => i.ThirdPartyId == request.ThirdPartyId.Value);

        if (request.Status is not null)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.OverdueOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(i => i.Status == InvoiceStatus.Approved && i.DueDate < today);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenBy(i => i.Number)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceListItemDto>(
            items.Select(i => i.ToListItemDto()).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
