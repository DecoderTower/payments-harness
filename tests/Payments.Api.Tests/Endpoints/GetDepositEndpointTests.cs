using System.Net;
using System.Net.Http.Json;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Endpoints;

public class GetDepositEndpointTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public async Task AS8_GetDeposit_ReturnsPersistedRecord()
    {
        var created = await CreateDepositAsync(accountReference: "acct-8", customerReference: "cust-8", amount: 2500, currency: "USD");

        var retrieved = await Client.GetFromJsonAsync<DepositResponse>($"/deposits/{created.DepositId}");

        Assert.NotNull(retrieved);
        Assert.Equal(created.DepositId, retrieved!.DepositId);
        Assert.Equal(2500, retrieved.Amount);
        Assert.Equal("USD", retrieved.Currency);
        Assert.Equal(created.ProviderReference, retrieved.ProviderReference);
        Assert.Equal(DepositStatus.PendingConfirmation, retrieved.Status);
    }

    [Fact]
    public async Task GetDeposit_WithUnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/deposits/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
