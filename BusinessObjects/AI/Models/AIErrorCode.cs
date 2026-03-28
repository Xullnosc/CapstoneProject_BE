namespace BusinessObjects.AI.Models;

public enum AIErrorCode
{
    Unknown,
    Disabled,
    InvalidApiKey,
    QuotaExceeded,
    RateLimited,
    Timeout,
    InvalidRequest,
    ProviderUnavailable,
    ContentFiltered,
    NotConfigured
}
