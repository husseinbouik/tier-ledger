using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TierLedger.Api.Models;
using TierLedger.Application.Invoices;
using TierLedger.Application.Invoices.GetPaymentProcess;
using TierLedger.Application.ThirdParties;
using TierLedger.Application.ThirdParties.GetOutstandingBalance;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.Processes;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.IntegrationTests;

public sealed class InvoiceLifecycleTests : IClassFixture<TierLedgerApiFactory>
{
    private readonly HttpClient _client;
    private readonly TierLedgerApiFactory _factory;

    public InvoiceLifecycleTests(TierLedgerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static RegisterThirdPartyRequest SampleThirdParty(string taxId) => new(
        "Atlas Office Supplies SARL",
        ThirdPartyType.Supplier,
        taxId,
        "contact@atlas.example",
        null,
        "12 Rue Gauthier",
        "Casablanca",
        "20250",
        "Morocco");

    private static async Task<T> PollAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> isDone,
        int timeoutSeconds = 25)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (true)
        {
            var value = await probe();
            if (isDone(value) || DateTime.UtcNow > deadline)
                return value;
            await Task.Delay(400);
        }
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullLifecycle_CreateSubmitApprove_PayInTwoInstalments_EndsPaid()
    {
        // Arrange: a third party and a draft invoice of 2 x 500 MAD + 20% VAT = 1 200 MAD gross.
        var thirdPartyResponse = await _client.PostAsJsonAsync(
            "/api/third-parties", SampleThirdParty($"ICE-{Guid.NewGuid():N}"[..20]), TierLedgerApiFactory.JsonOptions);
        thirdPartyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var thirdPartyId = (await thirdPartyResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var invoiceResponse = await _client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceRequest(
                thirdPartyId,
                new DateOnly(2026, 1, 10),
                new DateOnly(2026, 2, 10),
                "MAD",
                "Lifecycle test",
                new[] { new CreateInvoiceLineRequest("Consulting day", 2, 500m, 20m) }),
            TierLedgerApiFactory.JsonOptions);
        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoiceId = (await invoiceResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var draft = await _client.GetFromJsonAsync<InvoiceDto>(
            $"/api/invoices/{invoiceId}", TierLedgerApiFactory.JsonOptions);
        draft!.Status.Should().Be(InvoiceStatus.Draft);
        draft.Number.Should().MatchRegex(@"^INV-\d{4}-\d{4}$");
        draft.GrossAmount.Should().Be(1200m);

        // Act: submit and approve.
        (await _client.PostAsync($"/api/invoices/{invoiceId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsync($"/api/invoices/{invoiceId}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The payment process appears once the outbox dispatcher delivers InvoiceApproved.
        var process = await PollAsync(
            async () =>
            {
                var response = await _client.GetAsync($"/api/invoices/{invoiceId}/payment-process");
                return response.StatusCode == HttpStatusCode.OK
                    ? await response.Content.ReadFromJsonAsync<PaymentProcessDto>(TierLedgerApiFactory.JsonOptions)
                    : null;
            },
            p => p is not null);
        process.Should().NotBeNull();
        process!.Status.Should().Be(PaymentProcessStatus.AwaitingPayment);
        process.TotalDue.Should().Be(1200m);

        // First instalment: still approved, partially paid.
        var payment1 = await _client.PostAsJsonAsync(
            $"/api/invoices/{invoiceId}/payments",
            new RecordPaymentRequest(500m, PaymentMethod.BankTransfer, "TRX-001", null),
            TierLedgerApiFactory.JsonOptions);
        payment1.StatusCode.Should().Be(HttpStatusCode.Created);

        var afterFirst = await PollAsync(
            () => _client.GetFromJsonAsync<InvoiceDto>($"/api/invoices/{invoiceId}", TierLedgerApiFactory.JsonOptions),
            i => i!.TotalPaid == 500m);
        afterFirst!.Status.Should().Be(InvoiceStatus.Approved);
        afterFirst.OutstandingAmount.Should().Be(700m);

        // Second instalment settles the invoice: the process manager marks it Paid.
        var payment2 = await _client.PostAsJsonAsync(
            $"/api/invoices/{invoiceId}/payments",
            new RecordPaymentRequest(700m, PaymentMethod.BankTransfer, "TRX-002", null),
            TierLedgerApiFactory.JsonOptions);
        payment2.StatusCode.Should().Be(HttpStatusCode.Created);

        var paid = await PollAsync(
            () => _client.GetFromJsonAsync<InvoiceDto>($"/api/invoices/{invoiceId}", TierLedgerApiFactory.JsonOptions),
            i => i!.Status == InvoiceStatus.Paid);
        paid!.Status.Should().Be(InvoiceStatus.Paid);
        paid.PaidOnUtc.Should().NotBeNull();

        var completedProcess = await _client.GetFromJsonAsync<PaymentProcessDto>(
            $"/api/invoices/{invoiceId}/payment-process", TierLedgerApiFactory.JsonOptions);
        completedProcess!.Status.Should().Be(PaymentProcessStatus.Completed);
        completedProcess.AmountPaid.Should().Be(1200m);

        var balance = await _client.GetFromJsonAsync<OutstandingBalanceDto>(
            $"/api/third-parties/{thirdPartyId}/outstanding-balance", TierLedgerApiFactory.JsonOptions);
        balance!.OpenInvoiceCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateInvoice_ForUnknownThirdParty_Returns404ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceRequest(
                Guid.NewGuid(),
                new DateOnly(2026, 1, 10),
                new DateOnly(2026, 2, 10),
                "MAD",
                null,
                new[] { new CreateInvoiceLineRequest("X", 1, 10m, 20m) }),
            TierLedgerApiFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Resource not found");
    }

    [Fact]
    public async Task RegisterThirdParty_WithInvalidEmail_Returns400WithFieldErrors()
    {
        var request = SampleThirdParty($"ICE-{Guid.NewGuid():N}"[..20]) with { Email = "not-an-email" };

        var response = await _client.PostAsJsonAsync("/api/third-parties", request, TierLedgerApiFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Validation failed");
        body.Should().Contain("Email");
    }

    [Fact]
    public async Task SubmitInvoice_Twice_SecondCall_Returns409()
    {
        var thirdPartyResponse = await _client.PostAsJsonAsync(
            "/api/third-parties", SampleThirdParty($"ICE-{Guid.NewGuid():N}"[..20]), TierLedgerApiFactory.JsonOptions);
        var thirdPartyId = (await thirdPartyResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var invoiceResponse = await _client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceRequest(
                thirdPartyId,
                new DateOnly(2026, 1, 10),
                new DateOnly(2026, 2, 10),
                "MAD",
                null,
                new[] { new CreateInvoiceLineRequest("X", 1, 10m, 20m) }),
            TierLedgerApiFactory.JsonOptions);
        var invoiceId = (await invoiceResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        (await _client.PostAsync($"/api/invoices/{invoiceId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await _client.PostAsync($"/api/invoices/{invoiceId}/submit", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetInvoice_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/invoices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordPayment_OnDraftInvoice_Returns409()
    {
        var thirdPartyResponse = await _client.PostAsJsonAsync(
            "/api/third-parties", SampleThirdParty($"ICE-{Guid.NewGuid():N}"[..20]), TierLedgerApiFactory.JsonOptions);
        var thirdPartyId = (await thirdPartyResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var invoiceResponse = await _client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceRequest(
                thirdPartyId,
                new DateOnly(2026, 1, 10),
                new DateOnly(2026, 2, 10),
                "MAD",
                null,
                new[] { new CreateInvoiceLineRequest("X", 1, 10m, 20m) }),
            TierLedgerApiFactory.JsonOptions);
        var invoiceId = (await invoiceResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/invoices/{invoiceId}/payments",
            new RecordPaymentRequest(10m, PaymentMethod.Cash, null, null),
            TierLedgerApiFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
