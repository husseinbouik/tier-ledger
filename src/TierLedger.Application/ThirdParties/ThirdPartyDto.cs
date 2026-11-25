using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties;

public sealed record AddressDto(string Street, string City, string PostalCode, string Country);

public sealed record ThirdPartyDto(
    Guid Id,
    string Name,
    ThirdPartyType Type,
    string TaxId,
    string Email,
    string? Phone,
    AddressDto Address,
    bool IsActive,
    DateTime CreatedOnUtc);

public sealed record ThirdPartyListItemDto(
    Guid Id,
    string Name,
    ThirdPartyType Type,
    string TaxId,
    string Email,
    bool IsActive);

public static class ThirdPartyMappings
{
    public static ThirdPartyDto ToDto(this ThirdParty thirdParty) => new(
        thirdParty.Id,
        thirdParty.Name,
        thirdParty.Type,
        thirdParty.TaxId,
        thirdParty.Email,
        thirdParty.Phone,
        new AddressDto(
            thirdParty.Address.Street,
            thirdParty.Address.City,
            thirdParty.Address.PostalCode,
            thirdParty.Address.Country),
        thirdParty.IsActive,
        thirdParty.CreatedOnUtc);

    public static ThirdPartyListItemDto ToListItemDto(this ThirdParty thirdParty) => new(
        thirdParty.Id,
        thirdParty.Name,
        thirdParty.Type,
        thirdParty.TaxId,
        thirdParty.Email,
        thirdParty.IsActive);
}
