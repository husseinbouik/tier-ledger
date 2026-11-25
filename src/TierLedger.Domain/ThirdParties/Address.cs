using TierLedger.Domain.Common;

namespace TierLedger.Domain.ThirdParties;

/// <summary>Postal address value object, mapped as an owned entity by EF Core.</summary>
public sealed record Address
{
    public Address(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Address street is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Address city is required.");
        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Address country is required.");

        Street = street.Trim();
        City = city.Trim();
        PostalCode = postalCode?.Trim() ?? string.Empty;
        Country = country.Trim();
    }

    public string Street { get; init; }
    public string City { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }
}
