using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.GetThirdPartyById;

public sealed class GetThirdPartyByIdQueryHandler : IRequestHandler<GetThirdPartyByIdQuery, ThirdPartyDto>
{
    private readonly IApplicationDbContext _context;

    public GetThirdPartyByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ThirdPartyDto> Handle(GetThirdPartyByIdQuery request, CancellationToken cancellationToken)
    {
        var thirdParty = await _context.ThirdParties
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ThirdParty), request.Id);

        return thirdParty.ToDto();
    }
}
