using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.ThirdParties.GetThirdPartyById;
using TierLedger.Application.ThirdParties.RegisterThirdParty;
using TierLedger.Domain.Common;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.UnitTests.Application;

public sealed class ThirdPartyHandlersTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private static RegisterThirdPartyCommand SampleCommand(string taxId = "ICE-123") => new(
        "Atlas Office Supplies SARL",
        ThirdPartyType.Supplier,
        taxId,
        "contact@atlas.example",
        null,
        "12 Rue Gauthier",
        "Casablanca",
        "20250",
        "Morocco");

    [Fact]
    public async Task RegisterThirdParty_Persists_AndCanBeReadBack()
    {
        var id = await _fixture.Mediator.Send(SampleCommand());

        var dto = await _fixture.Mediator.Send(new GetThirdPartyByIdQuery(id));
        dto.Name.Should().Be("Atlas Office Supplies SARL");
        dto.IsActive.Should().BeTrue();
        dto.Address.City.Should().Be("Casablanca");

        var stored = await _fixture.Context.ThirdParties.SingleAsync(t => t.Id == id);
        stored.TaxId.Should().Be("ICE-123");

        // The registration event was written to the transactional outbox.
        var outboxCount = await _fixture.Context.OutboxMessages.CountAsync();
        outboxCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterThirdParty_WithDuplicateTaxId_ThrowsDomainException()
    {
        await _fixture.Mediator.Send(SampleCommand());

        var act = () => _fixture.Mediator.Send(SampleCommand());

        await act.Should().ThrowAsync<DomainException>().WithMessage("*tax id*already exists*");
    }
}
