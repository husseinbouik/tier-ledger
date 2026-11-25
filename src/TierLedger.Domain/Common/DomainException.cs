namespace TierLedger.Domain.Common;

/// <summary>
/// Raised when a business rule or a state-transition guard is violated.
/// The API layer maps it to HTTP 409 Conflict.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
