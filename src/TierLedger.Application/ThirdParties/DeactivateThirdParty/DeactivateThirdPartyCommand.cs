using MediatR;

namespace TierLedger.Application.ThirdParties.DeactivateThirdParty;

public sealed record DeactivateThirdPartyCommand(Guid Id) : IRequest;
