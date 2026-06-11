using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

public class DepositLifecycleTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS10_FullLifecycle_CreateConfirmRetrieveReconcile_IsConsistent()
    {
        var created = await CreateDepositAsync(accountReference: "acct-10", customerReference: "cust-10", amount: 5000, currency: "EUR");
        Assert.Equal(DepositStatus.PendingConfirmation, created.Status);
        Assert.StartsWith("pi_", created.ProviderReference);

        var confirmResponse = await Client.PostAsJsonAsync(
            $"/deposits/{created.DepositId}/confirm", new ConfirmDepositRequest("pm_card_visa", null));
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Succeeded, confirmed!.Status);
        Assert.Equal(created.ProviderReference, confirmed.ProviderReference);

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{created.DepositId}");
        Assert.Equal(DepositStatus.Succeeded, retrieved!.Status);
        Assert.Equal(created.ProviderReference, retrieved.ProviderReference);

        var reconcileResponse = await Client.PostAsync($"/deposits/{created.DepositId}/reconcile", content: null);
        var reconciled = await reconcileResponse.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Succeeded, reconciled!.Status);
        Assert.Equal(created.DepositId, reconciled.DepositId);
        Assert.StartsWith("pi_", reconciled.ProviderReference);
    }
}
