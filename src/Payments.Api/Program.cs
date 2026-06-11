using Microsoft.EntityFrameworkCore;
using Payments.Api.Endpoints;
using Payments.Api.Middleware;
using Payments.Api.Persistence;
using Payments.Api.Providers;
using Payments.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DepositOptions>(builder.Configuration.GetSection(DepositOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();

app.MapDepositEndpoints();

app.Run();

public partial class Program;
