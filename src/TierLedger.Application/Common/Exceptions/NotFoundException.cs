namespace TierLedger.Application.Common.Exceptions;

/// <summary>Raised when a requested entity does not exist. Mapped to HTTP 404 by the API layer.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}
