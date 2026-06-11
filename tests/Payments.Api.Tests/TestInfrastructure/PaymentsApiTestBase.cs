using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Api.Contracts;
using Payments.Api.Persistence;

namespace Payments.Api.Tests.TestInfrastructure;

public abstract class PaymentsApiTestBase(PaymentsApiFactory factory) : IClassFixture<PaymentsApiFactory>
{
    protected PaymentsApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected async Task<DepositResponse> CreateDepositAsync(
        string accountReference = "acct_test",
        string customerReference = "cust_test",
        long amount = 1000,
        string currency = "USD",
        string? idempotencyKey = null)
    {
        var request = new CreateDepositRequest(accountReference, customerReference, amount, currency, idempotencyKey ?? Guid.NewGuid().ToString());

        var response = await Client.PostAsJsonAsync("/deposits", request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DepositResponse>())!;
    }

    /// <summary>White-box lookup for deposits that never received an HTTP response with a body
    /// (e.g. a transient provider failure during create returns 502 with no depositId).</summary>
    protected async Task<Guid> GetDepositIdByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var deposit = await db.Deposits.SingleAsync(d => d.IdempotencyKey == idempotencyKey);
        return deposit.DepositId;
    }
}
