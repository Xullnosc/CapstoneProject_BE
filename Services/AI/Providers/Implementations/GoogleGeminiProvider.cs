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
    private const string DefaultModel = "gemini-1.5-pro";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIProviderType ProviderType => AIProviderType.GoogleGemini;
    public string ModelName => string.IsNullOrWhiteSpace(_settings.Model) ? DefaultModel : _settings.Model;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public GoogleGeminiProvider(ProviderSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AIException(AIErrorCode.InvalidApiKey, "Google Gemini API key is not configured.", "GoogleGemini");

        var start = DateTime.UtcNow;
        using var client = BuildClient();

        var body = BuildRequestBody(request);
        using var content = new StringContent(
            JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

        var endpoint = $"{BaseUrl}/v1beta/models/{ModelName}:generateContent?key={_settings.ApiKey}";

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIException(AIErrorCode.Timeout, "Google Gemini request timed out.", "GoogleGemini", isRetryable: true, ex);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            MapErrorResponse(response.StatusCode, responseJson);

        var parsed = JsonSerializer.Deserialize<GeminiResponse>(responseJson, _json)
            ?? throw new AIException(AIErrorCode.Unknown, "Failed to parse Gemini response.", "GoogleGemini");

        var text = parsed.Candidates?
            .FirstOrDefault()?.Content?.Parts?
            .FirstOrDefault(p => p.Text != null)?.Text
            ?? throw new AIException(AIErrorCode.Unknown, "Gemini returned no text content.", "GoogleGemini");

        return new AIResponse
        {
            Content = text,
            Provider = "GoogleGemini",
            Model = ModelName,
            FromCache = false,
            Latency = DateTime.UtcNow - start,
            Usage = new AIUsage
            {
                PromptTokens = parsed.UsageMetadata?.PromptTokenCount ?? 0,
                CompletionTokens = parsed.UsageMetadata?.CandidatesTokenCount ?? 0,
                EstimatedCostUsd = EstimateCost(
                    parsed.UsageMetadata?.PromptTokenCount ?? 0,
                    parsed.UsageMetadata?.CandidatesTokenCount ?? 0,
                    ModelName)
            }
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient("AI_GoogleGemini");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        return client;
    }

    private static object BuildRequestBody(AIRequest req)
    {
        // Gemini uses "user"/"model" roles (not "assistant")
        var contents = req.Messages
            .Where(m => m.Role != AIMessageRole.System)
            .Select(m => new
            {
                role = m.Role == AIMessageRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            })
            .ToList<object>();

        // Merge system prompt sources
        var systemParts = req.Messages
            .Where(m => m.Role == AIMessageRole.System)
            .Select(m => m.Content)
            .ToList();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
            systemParts.Insert(0, req.SystemPrompt);

        object? systemInstruction = systemParts.Count > 0
            ? new { parts = systemParts.Select(t => new { text = t }).ToArray() }
            : null;

        return new
        {
            systemInstruction,
            contents,
            generationConfig = new
            {
                temperature = req.Temperature,
                maxOutputTokens = req.MaxTokens
            }
        };
    }

    private static void MapErrorResponse(HttpStatusCode status, string body)
    {
        var msg = TryExtractMessage(body) ?? $"HTTP {(int)status}";
        throw status switch
        {
            HttpStatusCode.Unauthorized  => new AIException(AIErrorCode.InvalidApiKey, $"Gemini: {msg}", "GoogleGemini"),
            HttpStatusCode.Forbidden     => new AIException(AIErrorCode.InvalidApiKey, $"Gemini: {msg}", "GoogleGemini"),
            HttpStatusCode.TooManyRequests => new AIException(AIErrorCode.RateLimited, $"Gemini: {msg}", "GoogleGemini", isRetryable: true),
            >= HttpStatusCode.InternalServerError =>
                new AIException(AIErrorCode.ProviderUnavailable, $"Gemini server error: {msg}", "GoogleGemini", isRetryable: true),
            _ => new AIException(AIErrorCode.InvalidRequest, $"Gemini: {msg}", "GoogleGemini")
        };
    }

    private static string? TryExtractMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private static decimal EstimateCost(int prompt, int completion, string model)
    {
        // Gemini 1.5 pricing per 1M tokens (2025, ≤128K context)
        var (inputPer1M, outputPer1M) = model.ToLowerInvariant() switch
        {
            var m when m.Contains("gemini-1.5-pro")   => (1.25m, 5.00m),
            var m when m.Contains("gemini-1.5-flash")  => (0.075m, 0.30m),
            var m when m.Contains("gemini-2.0-flash")  => (0.10m, 0.40m),
            _ => (1.25m, 5.00m)
        };
        return (prompt / 1_000_000m * inputPer1M) + (completion / 1_000_000m * outputPer1M);
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
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
