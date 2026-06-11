using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

public class ConfirmDepositEndpointTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS5_ConfirmDeposit_Succeeded_StoresSucceededStatus()
    {
        var deposit = await CreateDepositAsync();

        var response = await Client.PostAsJsonAsync(
            $"/deposits/{deposit.DepositId}/confirm", new ConfirmDepositRequest("pm_card_visa", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Succeeded, body!.Status);

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.Succeeded, retrieved!.Status);
    }

    [Fact]
    public async Task AS6_ConfirmDeposit_RequiresAction_StoresActionRequiredWithNextAction()
    {
        var deposit = await CreateDepositAsync();

        var response = await Client.PostAsJsonAsync(
            $"/deposits/{deposit.DepositId}/confirm",
            new ConfirmDepositRequest("pm_card_authenticationRequired", "https://example.com/return"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.ActionRequired, body!.Status);
        Assert.NotNull(body.NextAction);
        Assert.False(string.IsNullOrEmpty(body.ClientSecret));

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.ActionRequired, retrieved!.Status);
    }

    [Fact]
    public async Task AS7_ConfirmDeposit_Declined_StoresFailedStatus()
    {
        var deposit = await CreateDepositAsync();

        var response = await Client.PostAsJsonAsync(
            $"/deposits/{deposit.DepositId}/confirm", new ConfirmDepositRequest("pm_card_chargeDeclined", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Failed, body!.Status);
        Assert.False(string.IsNullOrEmpty(body.FailureCode));
        Assert.False(string.IsNullOrEmpty(body.FailureMessage));
    }

    [Fact]
    public async Task ConfirmDeposit_OnTerminalDeposit_IsRejected()
    {
        var deposit = await CreateDepositAsync();
        await Client.PostAsJsonAsync($"/deposits/{deposit.DepositId}/confirm", new ConfirmDepositRequest("pm_card_visa", null));

        var secondConfirm = await Client.PostAsJsonAsync(
            $"/deposits/{deposit.DepositId}/confirm", new ConfirmDepositRequest("pm_card_visa", null));

        Assert.Equal(HttpStatusCode.Conflict, secondConfirm.StatusCode);
    }

    [Fact]
    public async Task ConfirmDeposit_WithoutProviderReference_IsRejected()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        Factory.FakeProvider.FailNextCreate();

        var createResponse = await Client.PostAsJsonAsync(
            "/deposits", new CreateDepositRequest("acct-x", "cust-x", 1000, "USD", idempotencyKey));
        Assert.Equal(HttpStatusCode.BadGateway, createResponse.StatusCode);

        var depositId = await GetDepositIdByIdempotencyKeyAsync(idempotencyKey);

        var confirmResponse = await Client.PostAsJsonAsync(
            $"/deposits/{depositId}/confirm", new ConfirmDepositRequest("pm_card_visa", null));

        Assert.Equal(HttpStatusCode.Conflict, confirmResponse.StatusCode);
    }
}
