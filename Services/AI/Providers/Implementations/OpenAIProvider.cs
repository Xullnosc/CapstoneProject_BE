using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessObjects.AI.Models;
using Services.AI.Configuration;

namespace Services.AI.Providers.Implementations;

/// <summary>
/// Adapter for the OpenAI Chat Completions API (v1).
/// Endpoint: POST https://api.openai.com/v1/chat/completions
/// BYOK: set API key via appsettings AI:Providers:OpenAI:ApiKey or the matching environment variable.
/// </summary>
internal sealed class OpenAIProvider : IAIProvider
{
    private const string DefaultBaseUrl = "https://api.openai.com";
    private const string DefaultModel = "gpt-4o";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIProviderType ProviderType => AIProviderType.OpenAI;
    public string ModelName =>
        string.IsNullOrWhiteSpace(_settings.Model) ? DefaultModel : _settings.Model;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public OpenAIProvider(ProviderSettings settings, IHttpClientFactory httpClientFactory)
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
                "OpenAI API key is not configured.",
                "OpenAI"
            );

        var start = DateTime.UtcNow;
        using var client = BuildClient();

        var body = BuildRequestBody(request);
        var json = JsonSerializer.Serialize(body, _json);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
            ? DefaultBaseUrl
            : _settings.BaseUrl.TrimEnd('/');
        HttpResponseMessage response;

        try
        {
            response = await client.PostAsync(
                $"{baseUrl}/v1/chat/completions",
                content,
                cancellationToken
            );
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIException(
                AIErrorCode.Timeout,
                "OpenAI request timed out.",
                "OpenAI",
                isRetryable: true,
                ex
            );
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            MapErrorResponse(response.StatusCode, responseJson);

        var parsed =
            JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson, _json)
            ?? throw new AIException(
                AIErrorCode.Unknown,
                "Failed to parse OpenAI response.",
                "OpenAI"
            );

        var choice =
            parsed.Choices?.FirstOrDefault()
            ?? throw new AIException(AIErrorCode.Unknown, "OpenAI returned no choices.", "OpenAI");

        return new AIResponse
        {
            Content = choice.Message?.Content ?? string.Empty,
            Provider = "OpenAI",
            Model = parsed.Model ?? ModelName,
            FromCache = false,
            Latency = DateTime.UtcNow - start,
            Usage = new AIUsage
            {
                PromptTokens = parsed.Usage?.PromptTokens ?? 0,
                CompletionTokens = parsed.Usage?.CompletionTokens ?? 0,
                EstimatedCostUsd = EstimateCost(
                    parsed.Usage?.PromptTokens ?? 0,
                    parsed.Usage?.CompletionTokens ?? 0,
                    ModelName
                ),
            },
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient("AI_OpenAI");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _settings.ApiKey
        );
        return client;
    }

    private object BuildRequestBody(AIRequest request)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });

        foreach (var m in request.Messages)
        {
            messages.Add(
                new
                {
                    role = m.Role switch
                    {
                        AIMessageRole.System => "system",
                        AIMessageRole.Assistant => "assistant",
                        _ => "user",
                    },
                    content = m.Content,
                }
            );
        }

        return new
        {
            model = ModelName,
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
        };
    }

    private static void MapErrorResponse(HttpStatusCode status, string body)
    {
        var msg = TryExtractErrorMessage(body) ?? $"HTTP {(int)status}";

        throw status switch
        {
            HttpStatusCode.Unauthorized => new AIException(
                AIErrorCode.InvalidApiKey,
                $"OpenAI: {msg}",
                "OpenAI"
            ),
            HttpStatusCode.TooManyRequests => new AIException(
                AIErrorCode.RateLimited,
                $"OpenAI: {msg}",
                "OpenAI",
                isRetryable: true
            ),
            HttpStatusCode.PaymentRequired => new AIException(
                AIErrorCode.QuotaExceeded,
                $"OpenAI: {msg}",
                "OpenAI"
            ),
            >= HttpStatusCode.InternalServerError => new AIException(
                AIErrorCode.ProviderUnavailable,
                $"OpenAI server error: {msg}",
                "OpenAI",
                isRetryable: true
            ),
            _ => new AIException(AIErrorCode.InvalidRequest, $"OpenAI: {msg}", "OpenAI"),
        };
    }

    private static string? TryExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (
                doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msgEl)
            )
                return msgEl.GetString();
        }
        catch
        { /* ignore */
        }
        return null;
    }

    private static decimal EstimateCost(int prompt, int completion, string model)
    {
        // Pricing per 1M tokens (approximate public rates, 2025)
        var (inputPer1M, outputPer1M) = model.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4o-mini") => (0.15m, 0.60m),
            var m when m.Contains("gpt-4o") => (2.50m, 10.00m),
            var m when m.Contains("gpt-4-turbo") => (10.00m, 30.00m),
            var m when m.Contains("gpt-3.5") => (0.50m, 1.50m),
            _ => (2.50m, 10.00m),
        };
        return (prompt / 1_000_000m * inputPer1M) + (completion / 1_000_000m * outputPer1M);
    }

    // ── Response DTOs (private, not part of public API) ──────────────────────

    private sealed class OpenAIChatResponse
    {
        public string? Model { get; set; }
        public List<ChatChoice>? Choices { get; set; }
        public TokenUsage? Usage { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        public string? Content { get; set; }
    }

    private sealed class TokenUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
