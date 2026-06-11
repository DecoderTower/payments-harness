namespace Payments.Api.Services;

/// <summary>A create-deposit request failed FR-1 validation.</summary>
public class DepositValidationException(IReadOnlyList<string> errors)
    : Exception("Deposit request validation failed.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>The same idempotencyKey was reused with a different normalized payload (FR-3, AS-4).</summary>
public class IdempotencyConflictException(string message) : Exception(message);

/// <summary>No deposit exists with the given depositId.</summary>
public class DepositNotFoundException(Guid depositId) : Exception($"Deposit '{depositId}' was not found.")
{
    public Guid DepositId { get; } = depositId;
}

/// <summary>Confirmation was attempted on a deposit already in a terminal status (FR-5).</summary>
public class TerminalDepositException(string message) : Exception(message);

/// <summary>Confirmation or reconciliation was attempted before a provider reference was linked (NFR-4).</summary>
public class DepositNotReadyException(string message) : Exception(message);
