using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Common;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.RegisterThirdParty;

public sealed class RegisterThirdPartyCommandHandler : IRequestHandler<RegisterThirdPartyCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public RegisterThirdPartyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(RegisterThirdPartyCommand request, CancellationToken cancellationToken)
    {
        var taxIdTaken = await _context.ThirdParties
            .AnyAsync(t => t.TaxId == request.TaxId.Trim(), cancellationToken);

        if (taxIdTaken)
            throw new DomainException($"A third party with tax id '{request.TaxId}' already exists.");

        var thirdParty = ThirdParty.Register(
            request.Name,
            request.Type,
            request.TaxId,
            request.Email,
            request.Phone,
            new Address(request.Street, request.City, request.PostalCode, request.Country));

        _context.ThirdParties.Add(thirdParty);
        await _context.SaveChangesAsync(cancellationToken);

        return thirdParty.Id;
    }
}
