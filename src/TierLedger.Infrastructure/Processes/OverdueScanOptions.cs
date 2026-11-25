namespace TierLedger.Infrastructure.Processes;

public sealed class OverdueScanOptions
{
    public const string SectionName = "OverdueScan";

    public bool Enabled { get; set; } = true;

    /// <summary>How often open payment processes are checked against their due dates.</summary>
    public int IntervalMinutes { get; set; } = 60;
}
