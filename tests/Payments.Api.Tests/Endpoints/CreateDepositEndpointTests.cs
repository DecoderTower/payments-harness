using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

public class CreateDepositEndpointTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS1_CreateValidDeposit_ReturnsPendingConfirmationWithProviderReference()
    {
        var request = new CreateDepositRequest("acct-1", "cust-1", 1000, "USD", Guid.NewGuid().ToString());

        var response = await Client.PostAsJsonAsync("/deposits", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.DepositId);
        Assert.False(string.IsNullOrEmpty(body.ProviderReference));
        Assert.Equal(DepositStatus.PendingConfirmation, body.Status);
    }

    [Theory]
    [InlineData("", "cust-2", 1000, "USD", "key-missing-account")]
    [InlineData("acct-2", "cust-2", 1000, "ZZZ", "key-bad-currency")]
    [InlineData("acct-2", "cust-2", 0, "USD", "key-non-positive-amount")]
    [InlineData("acct-2", "cust-2", 1000, "USD", "")]
    public async Task AS2_CreateDeposit_WithInvalidRequest_IsRejected(
        string accountReference, string customerReference, long amount, string currency, string idempotencyKey)
    {
        var request = new CreateDepositRequest(accountReference, customerReference, amount, currency, idempotencyKey);

        var response = await Client.PostAsJsonAsync("/deposits", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AS3_ReplayingSameCreateRequest_ReturnsOriginalDeposit()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateDepositRequest("acct-3", "cust-3", 1500, "USD", idempotencyKey);

        var first = await Client.PostAsJsonAsync("/deposits", request);
        var firstBody = await first.Content.ReadFromJsonAsync<DepositResponse>();

        var second = await Client.PostAsJsonAsync("/deposits", request);
        var secondBody = await second.Content.ReadFromJsonAsync<DepositResponse>();

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(firstBody!.DepositId, secondBody!.DepositId);
        Assert.Equal(firstBody.ProviderReference, secondBody.ProviderReference);
    }

    [Fact]
    public async Task AS4_ReplayingCreateRequestWithDifferentPayload_IsRejectedAsConflict()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var original = new CreateDepositRequest("acct-4", "cust-4", 1500, "USD", idempotencyKey);
        var conflicting = original with { Amount = 2000 };

        var firstResponse = await Client.PostAsJsonAsync("/deposits", original);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<DepositResponse>();

        var secondResponse = await Client.PostAsJsonAsync("/deposits", conflicting);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var unchanged = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{firstBody!.DepositId}");
        Assert.Equal(1500, unchanged!.Amount);
    }

    [Fact]
    public async Task CreateDeposit_WithDecimalAmount_IsRejected()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var json = $$"""
            {"accountReference":"acct-5","customerReference":"cust-5","amount":10.5,"currency":"USD","idempotencyKey":"{{idempotencyKey}}"}
            """;
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/deposits", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
