using TierLedger.Domain.Common;

namespace TierLedger.Domain.ThirdParties.Events;

public sealed record ThirdPartyRegistered(Guid ThirdPartyId, string Name, ThirdPartyType Type) : IDomainEvent;
