using MediatR;

namespace TierLedger.Application.ThirdParties.GetThirdPartyById;

public sealed record GetThirdPartyByIdQuery(Guid Id) : IRequest<ThirdPartyDto>;
