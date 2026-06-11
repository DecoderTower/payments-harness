namespace Payments.Api.Providers;

/// <summary>
/// A provider call failed transiently (timeout, connection failure, rate limit, or
/// provider-side server error). Per NFR-7 this is distinct from a provider business
/// failure (decline) and from an internal processing error.
/// </summary>
public class ProviderTransientException : Exception
{
    public ProviderTransientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
