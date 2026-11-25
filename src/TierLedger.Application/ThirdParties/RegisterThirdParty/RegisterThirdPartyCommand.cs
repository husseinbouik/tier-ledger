using MediatR;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.RegisterThirdParty;

public sealed record RegisterThirdPartyCommand(
    string Name,
    ThirdPartyType Type,
    string TaxId,
    string Email,
    string? Phone,
    string Street,
    string City,
    string PostalCode,
    string Country) : IRequest<Guid>;
