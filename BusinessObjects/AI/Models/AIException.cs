namespace BusinessObjects.AI.Models;

/// <summary>
/// Thrown by AI providers and the AIService orchestrator.
/// Callers should catch this to surface user-friendly error messages.
/// </summary>
public sealed class AIException : Exception
{
    public AIErrorCode Code { get; }
    public string ProviderName { get; }

    /// <summary>When true the operation can be retried (e.g. transient 5xx or network timeout).</summary>
    public bool IsRetryable { get; }

    public AIException(
        AIErrorCode code,
        string message,
        string providerName,
        bool isRetryable = false,
        Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        ProviderName = providerName;
        IsRetryable = isRetryable;
    }
}
