using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Application.Common.Models;

namespace TierLedger.Application.ThirdParties.ListThirdParties;

public sealed class ListThirdPartiesQueryHandler
    : IRequestHandler<ListThirdPartiesQuery, PagedResult<ThirdPartyListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public ListThirdPartiesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedResult<ThirdPartyListItemDto>> Handle(
        ListThirdPartiesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ThirdParties.AsNoTracking();

        if (request.Type is not null)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.IsActive is not null)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(t => t.Name.Contains(search) || t.TaxId.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ThirdPartyListItemDto>(
            items.Select(t => t.ToListItemDto()).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
