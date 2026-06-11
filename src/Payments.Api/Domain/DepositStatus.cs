using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payments.Api.Domain;

/// <summary>
/// Internal deposit lifecycle status as defined in
/// docs/specs/stripe-deposit-processing.md section 8.
/// </summary>
[JsonConverter(typeof(DepositStatusJsonConverter))]
public enum DepositStatus
{
    Received,
    PendingConfirmation,
    ActionRequired,
    Processing,
    Succeeded,
    Failed,
    Cancelled
}

/// <summary>
/// Serializes <see cref="DepositStatus"/> as UPPER_SNAKE_CASE (spec section 8) for both
/// server responses and any client deserializing with default options.
/// </summary>
public sealed class DepositStatusJsonConverter()
    : JsonStringEnumConverter<DepositStatus>(JsonNamingPolicy.SnakeCaseUpper);
