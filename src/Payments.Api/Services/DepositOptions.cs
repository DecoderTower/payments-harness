namespace Payments.Api.Services;

/// <summary>
/// Service-level deposit configuration (FR-1 currency support).
/// </summary>
public class DepositOptions
{
    public const string SectionName = "Deposits";

    public IReadOnlyList<string> SupportedCurrencies { get; set; } = ["USD", "EUR", "GBP"];
}
