using MediatR;

namespace TierLedger.Application.ThirdParties.UpdateThirdParty;

public sealed record UpdateThirdPartyCommand(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string Street,
    string City,
    string PostalCode,
    string Country) : IRequest;
