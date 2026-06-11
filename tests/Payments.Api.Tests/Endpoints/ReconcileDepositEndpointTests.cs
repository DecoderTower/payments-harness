using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

public class ReconcileDepositEndpointTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS9_ReconcileDeposit_PicksUpProviderSideStatusChange()
    {
        var deposit = await CreateDepositAsync();
        Factory.FakeProvider.SetStatus(deposit.ProviderReference!, "succeeded");

        var response = await Client.PostAsync($"/deposits/{deposit.DepositId}/reconcile", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DepositResponse>();
        Assert.Equal(DepositStatus.Succeeded, body!.Status);

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{deposit.DepositId}");
        Assert.Equal(DepositStatus.Succeeded, retrieved!.Status);
    }

    [Fact]
    public async Task ReconcileDeposit_WithUnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/deposits/{Guid.NewGuid()}/reconcile", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
