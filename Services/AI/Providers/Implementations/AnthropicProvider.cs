using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessObjects.AI.Models;
using Services.AI.Configuration;

namespace Services.AI.Providers.Implementations;

/// <summary>
/// Adapter for the Anthropic Messages API.
/// Endpoint: POST https://api.anthropic.com/v1/messages
/// BYOK: set API key via AI:Providers:Anthropic:ApiKey.
/// Docs: https://docs.anthropic.com/en/api/messages
/// </summary>
internal sealed class AnthropicProvider : IAIProvider
{
    private const string BaseUrl = "https://api.anthropic.com";
    private const string AnthropicVersion = "2023-06-01";
    private const string DefaultModel = "claude-3-5-sonnet-20241022";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIProviderType ProviderType => AIProviderType.Anthropic;
    public string ModelName => string.IsNullOrWhiteSpace(_settings.Model) ? DefaultModel : _settings.Model;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public AnthropicProvider(ProviderSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AIException(AIErrorCode.InvalidApiKey, "Anthropic API key is not configured.", "Anthropic");

        var start = DateTime.UtcNow;
        using var client = BuildClient();

        var body = BuildRequestBody(request);
        using var content = new StringContent(
            JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync($"{BaseUrl}/v1/messages", content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIException(AIErrorCode.Timeout, "Anthropic request timed out.", "Anthropic", isRetryable: true, ex);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            MapErrorResponse(response.StatusCode, responseJson);

        var parsed = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, _json)
            ?? throw new AIException(AIErrorCode.Unknown, "Failed to parse Anthropic response.", "Anthropic");

        if (parsed.Type == "error")
        {
            var errMsg = parsed.Error?.Message ?? "Unknown Anthropic error";
            throw new AIException(AIErrorCode.Unknown, $"Anthropic error: {errMsg}", "Anthropic");
        }

        var text = parsed.Content?.FirstOrDefault(c => c.Type == "text")?.Text
            ?? throw new AIException(AIErrorCode.Unknown, "Anthropic returned no text content.", "Anthropic");

        return new AIResponse
        {
            Content = text,
            Provider = "Anthropic",
            Model = parsed.Model ?? ModelName,
            FromCache = false,
            Latency = DateTime.UtcNow - start,
            Usage = new AIUsage
            {
                PromptTokens = parsed.Usage?.InputTokens ?? 0,
                CompletionTokens = parsed.Usage?.OutputTokens ?? 0,
                EstimatedCostUsd = EstimateCost(
                    parsed.Usage?.InputTokens ?? 0,
                    parsed.Usage?.OutputTokens ?? 0,
                    ModelName)
            }
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient("AI_Anthropic");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        client.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
        return client;
    }

    private object BuildRequestBody(AIRequest req)
    {
        // Anthropic uses a separate "system" field, not a system message in the array
        var messages = req.Messages
            .Where(m => m.Role != AIMessageRole.System)
            .Select(m => new
            {
                role = m.Role == AIMessageRole.Assistant ? "assistant" : "user",
                content = m.Content
            })
            .ToList<object>();

        // Merge inline system messages + dedicated SystemPrompt field
        var systemParts = req.Messages
            .Where(m => m.Role == AIMessageRole.System)
            .Select(m => m.Content)
            .ToList();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
            systemParts.Insert(0, req.SystemPrompt);

        var systemText = systemParts.Count > 0
            ? string.Join("\n\n", systemParts)
            : (string?)null;

        return new
        {
            model = ModelName,
            max_tokens = req.MaxTokens,
            temperature = (double)req.Temperature,
            system = systemText,
            messages
        };
    }

    private static void MapErrorResponse(HttpStatusCode status, string body)
    {
        var msg = TryExtractMessage(body) ?? $"HTTP {(int)status}";
        throw status switch
        {
            HttpStatusCode.Unauthorized    => new AIException(AIErrorCode.InvalidApiKey, $"Anthropic: {msg}", "Anthropic"),
            HttpStatusCode.TooManyRequests => new AIException(AIErrorCode.RateLimited, $"Anthropic: {msg}", "Anthropic", isRetryable: true),
            HttpStatusCode.PaymentRequired => new AIException(AIErrorCode.QuotaExceeded, $"Anthropic: {msg}", "Anthropic"),
            >= HttpStatusCode.InternalServerError =>
                new AIException(AIErrorCode.ProviderUnavailable, $"Anthropic server error: {msg}", "Anthropic", isRetryable: true),
            _ => new AIException(AIErrorCode.InvalidRequest, $"Anthropic: {msg}", "Anthropic")
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

    private static decimal EstimateCost(int inputTokens, int outputTokens, string model)
    {
        // Approximate Anthropic pricing per 1M tokens (2025)
        var (inputPer1M, outputPer1M) = model.ToLowerInvariant() switch
        {
            var m when m.Contains("claude-3-5-sonnet") => (3.00m, 15.00m),
            var m when m.Contains("claude-3-5-haiku")  => (0.80m,  4.00m),
            var m when m.Contains("claude-3-opus")     => (15.00m, 75.00m),
            var m when m.Contains("claude-3-haiku")    => (0.25m,  1.25m),
            _ => (3.00m, 15.00m)
        };
        return (inputTokens / 1_000_000m * inputPer1M) + (outputTokens / 1_000_000m * outputPer1M);
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class AnthropicResponse
    {
        public string? Type { get; set; }
        public string? Model { get; set; }
        public List<AnthropicContentBlock>? Content { get; set; }
        public AnthropicUsage? Usage { get; set; }
        public AnthropicError? Error { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    private sealed class AnthropicUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }

    private sealed class AnthropicError
    {
        public string? Message { get; set; }
    }
}
