namespace TierLedger.Infrastructure.Outbox;

/// <summary>
/// One row of the transactional outbox. Domain events are written here inside
/// the same transaction as the aggregate change, then dispatched in-process by
/// <see cref="OutboxDispatcher"/> after commit (at-least-once delivery).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Assembly-qualified type name of the domain event.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON payload of the domain event.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
