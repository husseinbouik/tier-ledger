using FluentAssertions;
using TierLedger.Domain.Common;
using TierLedger.Domain.ThirdParties;
using TierLedger.Domain.ThirdParties.Events;

namespace TierLedger.UnitTests.Domain;

public sealed class ThirdPartyTests
{
    private static ThirdParty RegisterSample() => ThirdParty.Register(
        "Atlas Office Supplies SARL",
        ThirdPartyType.Supplier,
        "ICE-000111222333444",
        "contact@atlas.example",
        null,
        new Address("12 Rue Gauthier", "Casablanca", "20250", "Morocco"));

    [Fact]
    public void Register_SetsFields_AndRaisesEvent()
    {
        var thirdParty = RegisterSample();

        thirdParty.IsActive.Should().BeTrue();
        thirdParty.Name.Should().Be("Atlas Office Supplies SARL");
        thirdParty.Type.Should().Be(ThirdPartyType.Supplier);
        thirdParty.Address.City.Should().Be("Casablanca");
        thirdParty.DomainEvents.Should().ContainSingle(e => e is ThirdPartyRegistered);
    }

    [Fact]
    public void Register_WithBlankName_Throws()
    {
        var act = () => ThirdParty.Register(
            " ",
            ThirdPartyType.Customer,
            "TAX-1",
            "a@b.example",
            null,
            new Address("1 Street", "Tangier", "90000", "Morocco"));

        act.Should().Throw<DomainException>().WithMessage("*name is required*");
    }

    [Fact]
    public void Address_WithBlankStreet_Throws()
    {
        var act = () => new Address("", "Tangier", "90000", "Morocco");

        act.Should().Throw<DomainException>().WithMessage("*street is required*");
    }

    [Fact]
    public void Deactivate_Twice_Throws()
    {
        var thirdParty = RegisterSample();
        thirdParty.Deactivate();
        thirdParty.IsActive.Should().BeFalse();

        var act = () => thirdParty.Deactivate();
        act.Should().Throw<DomainException>().WithMessage("*already deactivated*");
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var thirdParty = RegisterSample();

        thirdParty.Update(
            "Atlas Office Supplies SA",
            "hello@atlas.example",
            "+212 6 00 00 00 00",
            new Address("9 Avenue Mohammed V", "Rabat", "10000", "Morocco"));

        thirdParty.Name.Should().Be("Atlas Office Supplies SA");
        thirdParty.Email.Should().Be("hello@atlas.example");
        thirdParty.Address.City.Should().Be("Rabat");
        thirdParty.UpdatedOnUtc.Should().NotBeNull();
    }
}
