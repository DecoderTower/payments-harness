using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

/// <summary>Spec FR-12/FR-13, AS-11 to AS-18: cancelling a deposit that has not been confirmed.</summary>
public class CancelDepositEndpointTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS11_Cancel_PendingConfirmation_StoresCancelledWithoutClientSecret()
    {
        var deposit = await CreateDepositAsync();
        Assert.NotNull(deposit.ClientSecret);

        var response = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Cancelled, body!.Status);
        Assert.Equal(deposit.ProviderReference, body.ProviderReference);
        Assert.Null(body.ClientSecret);

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.Cancelled, retrieved!.Status);
        Assert.Null(retrieved.ClientSecret);

        Assert.Contains(Factory.Logs.Messages, m =>
            m.Contains("lifecycle event cancelled:")
            && m.Contains($"depositId={deposit.DepositId}")
            && m.Contains($"providerReference={deposit.ProviderReference}"));
    }

    [Fact]
    public async Task AS12_Cancel_ActionRequired_StoresCancelled()
    {
        var deposit = await CreateDepositAsync();
        await ConfirmAsync(deposit.DepositId, "pm_card_authenticationRequired");

        var response = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Cancelled, body!.Status);
        Assert.Null(body.ClientSecret);
        Assert.Null(body.NextAction);
    }

    [Fact]
    public async Task AS13_Cancel_AlreadyCancelled_ReturnsOkWithoutProviderCall()
    {
        var deposit = await CreateDepositAsync();
        (await CancelAsync(deposit.DepositId)).EnsureSuccessStatusCode();
        var callsAfterFirst = Factory.FakeProvider.CancelCallCount;

        var response = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Cancelled, body!.Status);
        Assert.Equal(callsAfterFirst, Factory.FakeProvider.CancelCallCount);
        Assert.Contains(Factory.Logs.Messages, m =>
            m.Contains("cancel_replayed") && m.Contains(deposit.DepositId.ToString()));
    }

    [Theory]
    [InlineData(DepositStatus.Processing)]
    [InlineData(DepositStatus.Succeeded)]
    [InlineData(DepositStatus.Failed)]
    public async Task AS14_Cancel_ProcessingSucceededOrFailed_ReturnsConflictAndLeavesDepositUnchanged(DepositStatus status)
    {
        var deposit = await CreateDepositAsync();
        switch (status)
        {
            case DepositStatus.Failed:
                await ConfirmAsync(deposit.DepositId, "pm_card_chargeDeclined");
                break;
            default:
                Factory.FakeProvider.SetStatus(deposit.ProviderReference!, status == DepositStatus.Processing ? "processing" : "succeeded");
                (await Client.PostAsync($"/deposits/{deposit.DepositId}/reconcile", content: null)).EnsureSuccessStatusCode();
                break;
        }

        var callsBefore = Factory.FakeProvider.CancelCallCount;

        var response = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(callsBefore, Factory.FakeProvider.CancelCallCount);
        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(status, retrieved!.Status);
    }

    [Fact]
    public async Task AS15_Cancel_ReceivedWithoutProviderReference_ReturnsConflict()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        Factory.FakeProvider.FailNextCreate();
        await Client.PostAsJsonAsync(
            "/deposits", new CreateDepositRequest("acct-c15", "cust-c15", 1000, "USD", idempotencyKey));
        var depositId = await GetDepositIdByIdempotencyKeyAsync(idempotencyKey);
        var callsBefore = Factory.FakeProvider.CancelCallCount;

        var response = await CancelAsync(depositId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(callsBefore, Factory.FakeProvider.CancelCallCount);
        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{depositId}");
        Assert.Equal(DepositStatus.Received, retrieved!.Status);
        Assert.Null(retrieved.ProviderReference);
    }

    [Fact]
    public async Task AS16_Cancel_UnknownDeposit_ReturnsNotFound()
    {
        var callsBefore = Factory.FakeProvider.CancelCallCount;

        var response = await CancelAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(callsBefore, Factory.FakeProvider.CancelCallCount);
    }

    [Fact]
    public async Task AS17_Cancel_WhenProviderRefuses_SyncsStatusAndReturnsConflict()
    {
        var deposit = await CreateDepositAsync();
        Factory.FakeProvider.SetStatus(deposit.ProviderReference!, "succeeded");

        var response = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.Succeeded, retrieved!.Status);
        Assert.Contains(Factory.Logs.Messages, m =>
            m.Contains("cancel_rejected") && m.Contains(deposit.DepositId.ToString()));
    }

    [Fact]
    public async Task AS18_Cancel_AfterAmbiguousOutcome_KeepsStatusThenRetryResolvesToCancelled()
    {
        var deposit = await CreateDepositAsync();
        Factory.FakeProvider.FailNextCancel(applyFirst: true);

        var first = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
        var afterFailure = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.PendingConfirmation, afterFailure!.Status);
        Assert.Contains(Factory.Logs.Messages, m =>
            m.Contains("cancel_failed_transient") && m.Contains(deposit.DepositId.ToString()));

        var retry = await CancelAsync(deposit.DepositId);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var body = await retry.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Cancelled, body!.Status);
        Assert.Equal(deposit.ProviderReference, body.ProviderReference);
    }

    [Fact]
    public async Task AS18_Reconcile_AfterAmbiguousCancel_ResolvesToCancelled()
    {
        var deposit = await CreateDepositAsync();
        Factory.FakeProvider.FailNextCancel(applyFirst: true);
        Assert.Equal(HttpStatusCode.BadGateway, (await CancelAsync(deposit.DepositId)).StatusCode);

        var reconcile = await Client.PostAsync($"/deposits/{deposit.DepositId}/reconcile", content: null);

        Assert.Equal(HttpStatusCode.OK, reconcile.StatusCode);
        var body = await reconcile.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Cancelled, body!.Status);
        Assert.Null(body.ClientSecret);
    }

    private Task<HttpResponseMessage> CancelAsync(Guid depositId) =>
        Client.PostAsync($"/deposits/{depositId}/cancel", content: null);

    private async Task ConfirmAsync(Guid depositId, string paymentMethodReference)
    {
        var response = await Client.PostAsJsonAsync(
            $"/deposits/{depositId}/confirm", new ConfirmDepositRequest(paymentMethodReference, null));
        response.EnsureSuccessStatusCode();
    }
}
