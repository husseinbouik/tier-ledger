using MediatR;

namespace TierLedger.Domain.Common;

/// <summary>
/// Marker for domain events. Events implement MediatR's <see cref="INotification"/>
/// (via the MediatR.Contracts package only, so the domain stays free of any
/// infrastructure dependency) which lets the outbox dispatcher publish them
/// in-process once the transaction has committed.
/// </summary>
public interface IDomainEvent : INotification
{
}
