using Microsoft.EntityFrameworkCore;
using Payments.Api.Endpoints;
using Payments.Api.Middleware;
using Payments.Api.Persistence;
using Payments.Api.Providers;
using Payments.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DepositOptions>(builder.Configuration.GetSection(DepositOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("PaymentsDb") ?? "Data Source=payments.db";
builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(connectionString));

// Provider selection is configuration-driven only (ADR-003); both implementations
// share the same DepositService business-logic path.
var providerType = builder.Configuration.GetValue<string>("PaymentProvider:Type") ?? "Fake";
if (string.Equals(providerType, "Stripe", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
}
else
{
    builder.Services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
}

builder.Services.AddScoped<DepositService>();

builder.Services.AddExceptionHandler<DepositExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// The schema is owned by EF Core migrations, applied here on every start in every
// environment (ADR-008). A failed migration throws, so the service does not start.
//
// WebApplicationFactory builds this host multiple times per test class (a throwaway
// probe build plus the real TestServer build), each running this top-level code. The
// probe builds all fall back to this same default connection string, so concurrent
// test classes would migrate the same SQLite file at once. Serializing here keeps the
// in-process calls ordered; EF Core's migration lock covers separate processes.
lock (Program.DatabaseInitLock)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDepositEndpoints();

app.Run();

public partial class Program
{
    private static readonly object DatabaseInitLock = new();
}
