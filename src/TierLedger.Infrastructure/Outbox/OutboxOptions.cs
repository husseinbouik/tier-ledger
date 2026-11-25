namespace TierLedger.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>How often the dispatcher polls for undispatched messages.</summary>
    public int PollIntervalSeconds { get; set; } = 5;
}
