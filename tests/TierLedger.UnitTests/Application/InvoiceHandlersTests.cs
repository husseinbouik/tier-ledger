using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Invoices.ApproveInvoice;
using TierLedger.Application.Invoices.CreateInvoice;
using TierLedger.Application.Invoices.GetInvoiceById;
using TierLedger.Application.Invoices.RecordPayment;
using TierLedger.Application.Invoices.SubmitInvoice;
using TierLedger.Application.ThirdParties.DeactivateThirdParty;
using TierLedger.Application.ThirdParties.GetOutstandingBalance;
using TierLedger.Application.ThirdParties.RegisterThirdParty;
using TierLedger.Domain.Common;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.UnitTests.Application;

public sealed class InvoiceHandlersTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> RegisterThirdPartyAsync(string taxId = "ICE-999", ThirdPartyType type = ThirdPartyType.Supplier)
        => await _fixture.Mediator.Send(new RegisterThirdPartyCommand(
            "Atlas Office Supplies SARL",
            type,
            taxId,
            "contact@atlas.example",
            null,
            "12 Rue Gauthier",
            "Casablanca",
            "20250",
            "Morocco"));

    private static CreateInvoiceCommand SampleInvoice(Guid thirdPartyId, string currency = "MAD", decimal unitPrice = 500m)
        => new(
            thirdPartyId,
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 2, 10),
            currency,
            null,
            new[] { new CreateInvoiceLineDto("Consulting day", 2, unitPrice, 20m) });

    [Fact]
    public async Task CreateInvoice_ForUnknownThirdParty_ThrowsNotFound()
    {
        var act = () => _fixture.Mediator.Send(SampleInvoice(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateInvoice_ForDeactivatedThirdParty_ThrowsDomainException()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();
        await _fixture.Mediator.Send(new DeactivateThirdPartyCommand(thirdPartyId));

        var act = () => _fixture.Mediator.Send(SampleInvoice(thirdPartyId));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task CreateInvoice_AssignsSequentialNumbers()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();

        var firstId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId));
        var secondId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId));

        var year = DateTime.UtcNow.Year;
        var first = await _fixture.Mediator.Send(new GetInvoiceByIdQuery(firstId));
        var second = await _fixture.Mediator.Send(new GetInvoiceByIdQuery(secondId));

        first.Number.Should().Be($"INV-{year}-0001");
        second.Number.Should().Be($"INV-{year}-0002");
        first.GrossAmount.Should().Be(1200m);
        first.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task SubmitAndApprove_TransitionsArePersisted()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();
        var invoiceId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId));

        await _fixture.Mediator.Send(new SubmitInvoiceCommand(invoiceId));
        await _fixture.Mediator.Send(new ApproveInvoiceCommand(invoiceId));

        var dto = await _fixture.Mediator.Send(new GetInvoiceByIdQuery(invoiceId));
        dto.Status.Should().Be(InvoiceStatus.Approved);
    }

    [Fact]
    public async Task RecordPayment_Overpayment_ThrowsDomainException()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();
        var invoiceId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId));
        await _fixture.Mediator.Send(new SubmitInvoiceCommand(invoiceId));
        await _fixture.Mediator.Send(new ApproveInvoiceCommand(invoiceId));

        var act = () => _fixture.Mediator.Send(
            new RecordPaymentCommand(invoiceId, 1200.01m, PaymentMethod.BankTransfer, null, null));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*exceeds the outstanding amount*");
    }

    [Fact]
    public async Task GetOutstandingBalance_GroupsByCurrency()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();

        var madId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId, "MAD", 500m));
        var eurId = await _fixture.Mediator.Send(SampleInvoice(thirdPartyId, "EUR", 100m));

        foreach (var id in new[] { madId, eurId })
        {
            await _fixture.Mediator.Send(new SubmitInvoiceCommand(id));
            await _fixture.Mediator.Send(new ApproveInvoiceCommand(id));
        }

        var balance = await _fixture.Mediator.Send(new GetOutstandingBalanceQuery(thirdPartyId));

        balance.OpenInvoiceCount.Should().Be(2);
        balance.Balances.Should().HaveCount(2);
        balance.Balances.Should().Contain(b => b.Currency == "MAD" && b.Amount == 1200m);
        balance.Balances.Should().Contain(b => b.Currency == "EUR" && b.Amount == 240m);
    }

    [Fact]
    public async Task CreateInvoice_WithEmptyLines_FailsValidation()
    {
        var thirdPartyId = await RegisterThirdPartyAsync();
        var command = new CreateInvoiceCommand(
            thirdPartyId,
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 2, 10),
            "MAD",
            null,
            Array.Empty<CreateInvoiceLineDto>());

        var act = () => _fixture.Mediator.Send(command);

        // Thrown by the ValidationBehaviour before the handler runs.
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
