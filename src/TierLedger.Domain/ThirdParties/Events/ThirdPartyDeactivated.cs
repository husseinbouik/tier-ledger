using TierLedger.Domain.Common;

namespace TierLedger.Domain.ThirdParties.Events;

public sealed record ThirdPartyDeactivated(Guid ThirdPartyId, string Name) : IDomainEvent;
