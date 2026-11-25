namespace TierLedger.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Domain events raised by the aggregate are
/// collected here and picked up by the persistence layer, which stores them in
/// the transactional outbox inside the same database transaction.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
