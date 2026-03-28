using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessObjects.AI.Models;
using Services.AI.Configuration;

namespace Services.AI.Providers.Implementations;

/// <summary>
/// Adapter for the Google Gemini generateContent API.
/// Endpoint: POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
/// BYOK: set API key via AI:Providers:GoogleGemini:ApiKey.
/// Docs: https://ai.google.dev/api/generate-content
/// </summary>
internal sealed class GoogleGeminiProvider : IAIProvider
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com";
    private const string DefaultModel = "gemini-2.5-pro";
    private const string FallbackModel = "gemini-2.5-flash";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly string[] _preferredModels =
    {
        "gemini-2.5-pro",
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite",
        "gemini-2.0-flash",
        "gemini-2.0-flash-lite",
        "gemini-1.5-pro",
        "gemini-1.5-flash",
        "gemini-3-pro-preview",
        "gemini-3-flash-preview",
        "gemini-3.1-pro-preview",
        "gemini-3.1-flash-lite-preview",
        "gemini-pro",
        "gemini-pro-vision",
    };

    public AIProviderType ProviderType => AIProviderType.GoogleGemini;
    public string ModelName => NormalizeModelName(_settings.Model);
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public GoogleGeminiProvider(ProviderSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AIResponse> CallAsync(
        AIRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConfigured)
            throw new AIException(
                AIErrorCode.InvalidApiKey,
                "Google Gemini API key is not configured.",
                "GoogleGemini"
            );

        var start = DateTime.UtcNow;
        using var client = BuildClient();

        var body = BuildRequestBody(request);
        var model = ModelName;
        var response = await PostGenerateContentAsync(client, model, body, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (
            (
                response.StatusCode == HttpStatusCode.NotFound
                || response.StatusCode == HttpStatusCode.BadRequest
            ) && LooksLikeModelUnavailable(responseJson)
        )
        {
            // Some model IDs are disabled/deprecated per API version; discover an available model first.
            var discoveredModel = await TryGetSupportedModelAsync(client, cancellationToken);
            var retryModel = discoveredModel ?? FallbackModel;

            if (!retryModel.Equals(model, StringComparison.OrdinalIgnoreCase))
            {
                model = retryModel;
                response.Dispose();
                response = await PostGenerateContentAsync(client, model, body, cancellationToken);
                responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }

        if (
            (
                response.StatusCode == HttpStatusCode.NotFound
                || response.StatusCode == HttpStatusCode.BadRequest
            )
            && LooksLikeModelUnavailable(responseJson)
            && !model.Equals(FallbackModel, StringComparison.OrdinalIgnoreCase)
        )
        {
            // Final defensive retry on fixed fallback if discovery failed or returned an unsupported ID.
            model = FallbackModel;
            response.Dispose();
            response = await PostGenerateContentAsync(client, model, body, cancellationToken);
            responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
            MapErrorResponse(response.StatusCode, responseJson);

        var parsed =
            JsonSerializer.Deserialize<GeminiResponse>(responseJson, _json)
            ?? throw new AIException(
                AIErrorCode.Unknown,
                "Failed to parse Gemini response.",
                "GoogleGemini"
            );

        if (!string.IsNullOrWhiteSpace(parsed.PromptFeedback?.BlockReason))
        {
            var blockReason = parsed.PromptFeedback.BlockReason;
            var blockDetail = parsed.PromptFeedback.BlockReasonMessage;
            throw new AIException(
                AIErrorCode.ContentFiltered,
                string.IsNullOrWhiteSpace(blockDetail)
                    ? $"Gemini blocked the prompt ({blockReason})."
                    : $"Gemini blocked the prompt ({blockReason}): {blockDetail}",
                "GoogleGemini"
            );
        }

        var text = parsed
            .Candidates?.SelectMany(c => c.Content?.Parts ?? Enumerable.Empty<GeminiPart>())
            .Select(p => p.Text)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(text))
        {
            var finishReason = parsed.Candidates?.FirstOrDefault()?.FinishReason;
            if (
                !string.IsNullOrWhiteSpace(finishReason)
                && (
                    finishReason.Equals("SAFETY", StringComparison.OrdinalIgnoreCase)
                    || finishReason.Equals("BLOCKLIST", StringComparison.OrdinalIgnoreCase)
                    || finishReason.Equals("PROHIBITED_CONTENT", StringComparison.OrdinalIgnoreCase)
                    || finishReason.Equals("RECITATION", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                throw new AIException(
                    AIErrorCode.ContentFiltered,
                    $"Gemini blocked the response ({finishReason}).",
                    "GoogleGemini"
                );
            }

            throw new AIException(
                AIErrorCode.InvalidRequest,
                $"Gemini returned no text content (finishReason: {finishReason ?? "unknown"}).",
                "GoogleGemini"
            );
        }

        return new AIResponse
        {
            Content = text,
            Provider = "GoogleGemini",
            Model = model,
            FromCache = false,
            Latency = DateTime.UtcNow - start,
            Usage = new AIUsage
            {
                PromptTokens = parsed.UsageMetadata?.PromptTokenCount ?? 0,
                CompletionTokens = parsed.UsageMetadata?.CandidatesTokenCount ?? 0,
                EstimatedCostUsd = EstimateCost(
                    parsed.UsageMetadata?.PromptTokenCount ?? 0,
                    parsed.UsageMetadata?.CandidatesTokenCount ?? 0,
                    model
                ),
            },
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient("AI_GoogleGemini");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        return client;
    }

    private async Task<HttpResponseMessage> PostGenerateContentAsync(
        HttpClient client,
        string model,
        object body,
        CancellationToken cancellationToken
    )
    {
        using var content = new StringContent(
            JsonSerializer.Serialize(body, _json),
            Encoding.UTF8,
            "application/json"
        );

        var endpoint = $"{BaseUrl}/v1beta/models/{model}:generateContent?key={_settings.ApiKey}";

        try
        {
            return await client.PostAsync(endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIException(
                AIErrorCode.Timeout,
                "Google Gemini request timed out.",
                "GoogleGemini",
                isRetryable: true,
                ex
            );
        }
    }

    private static object BuildRequestBody(AIRequest req)
    {
        // Gemini uses "user"/"model" roles (not "assistant")
        var contents = req
            .Messages.Where(m => m.Role != AIMessageRole.System)
            .Select(m => new
            {
                role = m.Role == AIMessageRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Content } },
            })
            .ToList<object>();

        // Merge system prompt sources
        var systemParts = req
            .Messages.Where(m => m.Role == AIMessageRole.System)
            .Select(m => m.Content)
            .ToList();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
            systemParts.Insert(0, req.SystemPrompt);

        object? systemInstruction =
            systemParts.Count > 0
                ? new { parts = systemParts.Select(t => new { text = t }).ToArray() }
                : null;

        return new
        {
            systemInstruction,
            contents,
            generationConfig = new
            {
                temperature = req.Temperature,
                maxOutputTokens = req.MaxTokens,
            },
        };
    }

    private static void MapErrorResponse(HttpStatusCode status, string body)
    {
        var msg = TryExtractMessage(body) ?? $"HTTP {(int)status}";
        throw status switch
        {
            HttpStatusCode.Unauthorized => new AIException(
                AIErrorCode.InvalidApiKey,
                $"Gemini: {msg}",
                "GoogleGemini"
            ),
            HttpStatusCode.Forbidden => new AIException(
                AIErrorCode.InvalidApiKey,
                $"Gemini: {msg}",
                "GoogleGemini"
            ),
            HttpStatusCode.TooManyRequests when LooksLikeQuotaExceeded(msg) => new AIException(
                AIErrorCode.QuotaExceeded,
                $"Gemini: {msg}",
                "GoogleGemini"
            ),
            HttpStatusCode.TooManyRequests => new AIException(
                AIErrorCode.RateLimited,
                $"Gemini: {msg}",
                "GoogleGemini",
                isRetryable: true
            ),
            >= HttpStatusCode.InternalServerError => new AIException(
                AIErrorCode.ProviderUnavailable,
                $"Gemini server error: {msg}",
                "GoogleGemini",
                isRetryable: true
            ),
            _ => new AIException(AIErrorCode.InvalidRequest, $"Gemini: {msg}", "GoogleGemini"),
        };
    }

    private static string? TryExtractMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (
                doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var m)
            )
                return m.GetString();
        }
        catch
        { /* ignore */
        }
        return null;
    }

    private async Task<string?> TryGetSupportedModelAsync(
        HttpClient client,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var endpoint = $"{BaseUrl}/v1beta/models?key={_settings.ApiKey}";
            var response = await client.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var list = JsonSerializer.Deserialize<GeminiModelListResponse>(json, _json);
            var candidates = list
                ?.Models?.Where(m =>
                    m.SupportedGenerationMethods is not null
                    && m.SupportedGenerationMethods.Any(x =>
                        string.Equals(x, "generateContent", StringComparison.OrdinalIgnoreCase)
                    )
                )
                .Select(m => NormalizeModelName(m.Name))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (candidates is null || candidates.Count == 0)
                return null;

            foreach (var preferred in _preferredModels)
            {
                var hit = candidates.FirstOrDefault(c =>
                    c.Equals(preferred, StringComparison.OrdinalIgnoreCase)
                );
                if (!string.IsNullOrWhiteSpace(hit))
                    return hit;
            }

            return candidates[0];
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeModelName(string? model)
    {
        var normalized = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();

        if (normalized.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["models/".Length..];

        const string generateContentSuffix = ":generateContent";
        if (normalized.EndsWith(generateContentSuffix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^generateContentSuffix.Length];

        return normalized;
    }

    private static bool LooksLikeModelUnavailable(string json)
    {
        var message = TryExtractMessage(json);
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not supported", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
            || message.Contains("closed to new users", StringComparison.OrdinalIgnoreCase)
            || message.Contains("deprecated", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ListModels", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeQuotaExceeded(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("check your plan and billing", StringComparison.OrdinalIgnoreCase)
            || message.Contains("free_tier", StringComparison.OrdinalIgnoreCase)
            || message.Contains("limit: 0", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal EstimateCost(int prompt, int completion, string model)
    {
        // Gemini 1.5 pricing per 1M tokens (2025, ≤128K context)
        var (inputPer1M, outputPer1M) = model.ToLowerInvariant() switch
        {
            var m when m.Contains("gemini-1.5-pro") => (1.25m, 5.00m),
            var m when m.Contains("gemini-1.5-flash") => (0.075m, 0.30m),
            var m when m.Contains("gemini-2.0-flash") => (0.10m, 0.40m),
            _ => (1.25m, 5.00m),
        };
        return (prompt / 1_000_000m * inputPer1M) + (completion / 1_000_000m * outputPer1M);
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    private sealed class GeminiModelListResponse
    {
        public List<GeminiModelInfo>? Models { get; set; }
    }

    private sealed class GeminiModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<string>? SupportedGenerationMethods { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    private sealed class GeminiPromptFeedback
    {
        public string? BlockReason { get; set; }
        public string? BlockReasonMessage { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }

    private sealed class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
    }
}
