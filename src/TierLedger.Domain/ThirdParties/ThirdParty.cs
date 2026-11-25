using TierLedger.Domain.Common;
using TierLedger.Domain.ThirdParties.Events;

namespace TierLedger.Domain.ThirdParties;

/// <summary>
/// A third party (<i>tiers</i>): a customer, a supplier, or both.
/// Invoices are issued against third parties; a deactivated third party can no
/// longer receive new invoices (enforced by the application layer).
/// </summary>
public sealed class ThirdParty : AggregateRoot
{
    private ThirdParty()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ThirdPartyType Type { get; private set; }

    /// <summary>Tax identification number (e.g. ICE in Morocco, SIRET in France). Unique.</summary>
    public string TaxId { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public Address Address { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static ThirdParty Register(
        string name,
        ThirdPartyType type,
        string taxId,
        string email,
        string? phone,
        Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Third-party name is required.");
        if (string.IsNullOrWhiteSpace(taxId))
            throw new DomainException("Third-party tax id is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Third-party email is required.");
        ArgumentNullException.ThrowIfNull(address);

        var thirdParty = new ThirdParty
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            TaxId = taxId.Trim(),
            Email = email.Trim(),
            Phone = phone?.Trim(),
            Address = address,
            IsActive = true,
            CreatedOnUtc = DateTime.UtcNow,
        };

        thirdParty.RaiseDomainEvent(new ThirdPartyRegistered(thirdParty.Id, thirdParty.Name, thirdParty.Type));
        return thirdParty;
    }

    public void Update(string name, string email, string? phone, Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Third-party name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Third-party email is required.");
        ArgumentNullException.ThrowIfNull(address);

        Name = name.Trim();
        Email = email.Trim();
        Phone = phone?.Trim();
        Address = address;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException($"Third party '{Name}' is already deactivated.");

        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ThirdPartyDeactivated(Id, Name));
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException($"Third party '{Name}' is already active.");

        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
