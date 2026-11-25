using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.DeactivateThirdParty;

public sealed class DeactivateThirdPartyCommandHandler : IRequestHandler<DeactivateThirdPartyCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateThirdPartyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeactivateThirdPartyCommand request, CancellationToken cancellationToken)
    {
        var thirdParty = await _context.ThirdParties
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ThirdParty), request.Id);

        thirdParty.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
