using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.UpdateThirdParty;

public sealed class UpdateThirdPartyCommandHandler : IRequestHandler<UpdateThirdPartyCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateThirdPartyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateThirdPartyCommand request, CancellationToken cancellationToken)
    {
        var thirdParty = await _context.ThirdParties
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ThirdParty), request.Id);

        thirdParty.Update(
            request.Name,
            request.Email,
            request.Phone,
            new Address(request.Street, request.City, request.PostalCode, request.Country));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
