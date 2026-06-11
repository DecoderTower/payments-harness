using Payments.Api.Contracts;
using Payments.Api.Services;

namespace Payments.Api.Endpoints;

/// <summary>HTTP API surface for deposits (spec section 7).</summary>
public static class DepositEndpoints
{
    public static IEndpointRouteBuilder MapDepositEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/deposits");

        group.MapPost("/", async (CreateDepositRequest request, DepositService service, CancellationToken cancellationToken) =>
        {
            var deposit = await service.CreateAsync(
                new CreateDepositCommand(
                    request.AccountReference,
                    request.CustomerReference,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey),
                cancellationToken);

            return Results.Created($"/deposits/{deposit.DepositId}", DepositResponse.FromDeposit(deposit));
        });

        group.MapPost("/{depositId:guid}/confirm", async (Guid depositId, ConfirmDepositRequest request, DepositService service, CancellationToken cancellationToken) =>
        {
            var deposit = await service.ConfirmAsync(
                depositId,
                new ConfirmDepositCommand(request.PaymentMethodReference, request.ReturnUrl),
                cancellationToken);

            return Results.Ok(DepositResponse.FromDeposit(deposit));
        });

        group.MapGet("/{depositId:guid}", async (Guid depositId, DepositService service, CancellationToken cancellationToken) =>
        {
            var deposit = await service.GetAsync(depositId, cancellationToken);

            return deposit is null ? Results.NotFound() : Results.Ok(DepositResponse.FromDeposit(deposit));
        });

        group.MapPost("/{depositId:guid}/reconcile", async (Guid depositId, DepositService service, CancellationToken cancellationToken) =>
        {
            var deposit = await service.ReconcileAsync(depositId, cancellationToken);

            return Results.Ok(DepositResponse.FromDeposit(deposit));
        });

        return app;
    }
}
