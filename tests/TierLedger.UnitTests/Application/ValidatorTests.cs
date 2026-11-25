using FluentAssertions;
using FluentValidation.TestHelper;
using TierLedger.Application.Invoices.CreateInvoice;
using TierLedger.Application.ThirdParties.RegisterThirdParty;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.UnitTests.Application;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateInvoiceValidator_FlagsBadCurrencyAndDates()
    {
        var validator = new CreateInvoiceCommandValidator();
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 1, 10),
            "MADX",
            null,
            new[] { new CreateInvoiceLineDto("X", 1, 10m, 20m) });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void CreateInvoiceValidator_FlagsInvalidLineValues()
    {
        var validator = new CreateInvoiceCommandValidator();
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 2, 10),
            "MAD",
            null,
            new[] { new CreateInvoiceLineDto("", 0, -5m, 120m) });

        var result = validator.TestValidate(command);

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisterThirdPartyValidator_FlagsBadEmail_AndAcceptsValidCommand()
    {
        var validator = new RegisterThirdPartyCommandValidator();

        var invalid = new RegisterThirdPartyCommand(
            "Atlas", ThirdPartyType.Supplier, "ICE-1", "not-an-email", null,
            "Street", "City", "10000", "Morocco");
        validator.TestValidate(invalid).ShouldHaveValidationErrorFor(x => x.Email);

        var valid = invalid with { Email = "contact@atlas.example" };
        validator.TestValidate(valid).IsValid.Should().BeTrue();
    }
}
