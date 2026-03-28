using System.Text.RegularExpressions;
using BusinessObjects.AI.Models;

namespace Services.AI.Validation;

/// <summary>
/// Validates AI requests before they reach the provider.
/// Catches prompt injection attempts, enforces length constraints,
/// and estimates token count.
/// </summary>
internal static class PromptValidator
{
    /// <summary>Max allowed characters across all messages in a single request.</summary>
    private const int MaxTotalCharacters = 200_000;

    /// <summary>Max characters in a single message.</summary>
    private const int MaxMessageCharacters = 100_000;

    /// <summary>Approximate tokens ≈ chars / 4 (conservative OpenAI estimate).</summary>
    private const double CharsPerToken = 4.0;

    // Common prompt-injection pattern fragments (case-insensitive)
    private static readonly Regex[] _injectionPatterns =
    {
        new(
            @"\bignore\s+(all\s+)?(previous|prior|above)\s+instructions?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        ),
        new(
            @"\bforget\s+(your\s+)?(previous|all|prior)\s+instructions?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        ),
        new(@"\byou\s+are\s+now\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bact\s+as\s+(if\s+you\s+are|a)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(
            @"\bdo\s+not\s+follow\s+(your\s+)?(guidelines?|instructions?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        ),
        new(@"\bpretend\s+(you\s+are|to\s+be)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdeveloper\s+mode\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bjailbreak\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    /// <summary>
    /// Validates the request.
    /// Throws <see cref="AIException"/> with <see cref="AIErrorCode.InvalidRequest"/>
    /// or <see cref="AIErrorCode.ContentFiltered"/> when validation fails.
    /// </summary>
    public static void Validate(AIRequest request)
    {
        if (request.Messages is null || request.Messages.Count == 0)
            throw new AIException(
                AIErrorCode.InvalidRequest,
                "AI request must contain at least one message.",
                "Validator"
            );

        var hasUserMessage = request.Messages.Any(m => m.Role == AIMessageRole.User);
        if (!hasUserMessage)
            throw new AIException(
                AIErrorCode.InvalidRequest,
                "AI request must contain at least one User message.",
                "Validator"
            );

        if (request.Temperature is < 0 or > 2)
            throw new AIException(
                AIErrorCode.InvalidRequest,
                "Temperature must be between 0 and 2.",
                "Validator"
            );

        if (request.MaxTokens is <= 0 or > 128_000)
            throw new AIException(
                AIErrorCode.InvalidRequest,
                "MaxTokens must be between 1 and 128,000.",
                "Validator"
            );

        var totalChars = 0;
        foreach (var message in request.Messages)
        {
            if (string.IsNullOrEmpty(message.Content))
                continue;

            if (message.Content.Length > MaxMessageCharacters)
                throw new AIException(
                    AIErrorCode.InvalidRequest,
                    $"A single message exceeds the {MaxMessageCharacters:N0}-character limit.",
                    "Validator"
                );

            totalChars += message.Content.Length;

            if (message.Role == AIMessageRole.User)
                CheckInjectionPatterns(message.Content);
        }

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            totalChars += request.SystemPrompt.Length;

        if (totalChars > MaxTotalCharacters)
            throw new AIException(
                AIErrorCode.InvalidRequest,
                $"Total request length ({totalChars:N0} chars) exceeds the {MaxTotalCharacters:N0}-character limit.",
                "Validator"
            );
    }

    /// <summary>
    /// Estimates the number of tokens in a request (used for pre-call cost guarding).
    /// Formula: total characters / 4 (conservative approximation).
    /// </summary>
    public static int EstimateTokens(AIRequest request)
    {
        var chars =
            request.Messages.Sum(m => m.Content?.Length ?? 0) + (request.SystemPrompt?.Length ?? 0);
        return (int)Math.Ceiling(chars / CharsPerToken) + 10; // +10 overhead
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void CheckInjectionPatterns(string content)
    {
        foreach (var pattern in _injectionPatterns)
        {
            if (pattern.IsMatch(content))
                throw new AIException(
                    AIErrorCode.ContentFiltered,
                    "Request was blocked: potential prompt injection detected.",
                    "Validator"
                );
        }
    }
}
