using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

/// <summary>
/// NFR-4: covers the "ambiguous outcome" recovery path, where a transient failure
/// during PaymentIntent creation leaves a deposit RECEIVED with no providerReference,
/// and a later replay links it without creating a duplicate PaymentIntent.
/// </summary>
public class AmbiguousOutcomeTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task CreateDeposit_WhenProviderLinkFailsTransiently_PersistsReceivedDepositAndReturnsBadGateway()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        Factory.FakeProvider.FailNextCreate();

        var response = await Client.PostAsJsonAsync(
            "/deposits", new CreateDepositRequest("acct-amb", "cust-amb", 1000, "USD", idempotencyKey));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var depositId = await GetDepositIdByIdempotencyKeyAsync(idempotencyKey);
        var deposit = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{depositId}");
        Assert.Equal(DepositStatus.Received, deposit!.Status);
        Assert.Null(deposit.ProviderReference);
    }

    [Fact]
    public async Task ReconcileDeposit_WithoutProviderReference_IsRejected()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        Factory.FakeProvider.FailNextCreate();

        await Client.PostAsJsonAsync(
            "/deposits", new CreateDepositRequest("acct-amb2", "cust-amb2", 1000, "USD", idempotencyKey));

        var depositId = await GetDepositIdByIdempotencyKeyAsync(idempotencyKey);

        var reconcileResponse = await Client.PostAsync($"/deposits/{depositId}/reconcile", content: null);

        Assert.Equal(HttpStatusCode.Conflict, reconcileResponse.StatusCode);
    }

    [Fact]
    public async Task ReplayingCreateAfterTransientFailure_LinksProviderReferenceWithoutDuplicateIntent()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateDepositRequest("acct-amb3", "cust-amb3", 1000, "USD", idempotencyKey);
        Factory.FakeProvider.FailNextCreate();

        var first = await Client.PostAsJsonAsync("/deposits", request);
        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);

        var second = await Client.PostAsJsonAsync("/deposits", request);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var secondBody = await second.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.False(string.IsNullOrEmpty(secondBody!.ProviderReference));
        Assert.Equal(DepositStatus.PendingConfirmation, secondBody.Status);

        var depositId = await GetDepositIdByIdempotencyKeyAsync(idempotencyKey);
        Assert.Equal(secondBody.DepositId, depositId);
    }
}
