using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessObjects.AI.Models;
using Services.AI.Configuration;

namespace Services.AI.Providers.Implementations;

/// <summary>
/// Adapter for Azure OpenAI Chat Completions.
/// Endpoint: POST https://&lt;resource&gt;.openai.azure.com/openai/deployments/&lt;deployment&gt;/chat/completions?api-version=&lt;version&gt;
/// BYOK: set API key via AI:Providers:AzureOpenAI:ApiKey. BaseUrl and DeploymentName are required.
/// </summary>
internal sealed class AzureOpenAIProvider : IAIProvider
{
    private const string DefaultApiVersion = "2024-05-01-preview";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIProviderType ProviderType => AIProviderType.AzureOpenAI;
    public string ModelName => _settings.DeploymentName ?? _settings.Model;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(_settings.BaseUrl) &&
        !string.IsNullOrWhiteSpace(_settings.DeploymentName ?? _settings.Model);

    public AzureOpenAIProvider(ProviderSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AIException(AIErrorCode.InvalidApiKey,
                "Azure OpenAI requires ApiKey, BaseUrl, and DeploymentName to be configured.", "AzureOpenAI");

        var start = DateTime.UtcNow;
        using var client = BuildClient();

        var body = BuildRequestBody(request);
        using var content = new StringContent(
            JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

        var apiVersion = string.IsNullOrWhiteSpace(_settings.ApiVersion)
            ? DefaultApiVersion
            : _settings.ApiVersion;
        var deployment = _settings.DeploymentName ?? _settings.Model;
        var baseUrl = _settings.BaseUrl!.TrimEnd('/');
        var endpoint = $"{baseUrl}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AIException(AIErrorCode.Timeout, "Azure OpenAI request timed out.", "AzureOpenAI", isRetryable: true, ex);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            MapErrorResponse(response.StatusCode, responseJson);

        var parsed = JsonSerializer.Deserialize<AzureChatResponse>(responseJson, _json)
            ?? throw new AIException(AIErrorCode.Unknown, "Failed to parse Azure OpenAI response.", "AzureOpenAI");

        var choice = parsed.Choices?.FirstOrDefault()
            ?? throw new AIException(AIErrorCode.Unknown, "Azure OpenAI returned no choices.", "AzureOpenAI");

        return new AIResponse
        {
            Content = choice.Message?.Content ?? string.Empty,
            Provider = "AzureOpenAI",
            Model = parsed.Model ?? deployment,
            FromCache = false,
            Latency = DateTime.UtcNow - start,
            Usage = new AIUsage
            {
                PromptTokens = parsed.Usage?.PromptTokens ?? 0,
                CompletionTokens = parsed.Usage?.CompletionTokens ?? 0,
                EstimatedCostUsd = 0m // Azure pricing is customer-specific
            }
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient("AI_AzureOpenAI");
        client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        client.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
        return client;
    }

    private object BuildRequestBody(AIRequest req)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
            messages.Add(new { role = "system", content = req.SystemPrompt });

        foreach (var m in req.Messages)
        {
            messages.Add(new
            {
                role = m.Role switch
                {
                    AIMessageRole.System    => "system",
                    AIMessageRole.Assistant => "assistant",
                    _                       => "user"
                },
                content = m.Content
            });
        }

        return new { messages, temperature = req.Temperature, max_tokens = req.MaxTokens };
    }

    private static void MapErrorResponse(HttpStatusCode status, string body)
    {
        var msg = TryExtractMessage(body) ?? $"HTTP {(int)status}";
        throw status switch
        {
            HttpStatusCode.Unauthorized    => new AIException(AIErrorCode.InvalidApiKey, $"AzureOpenAI: {msg}", "AzureOpenAI"),
            HttpStatusCode.TooManyRequests => new AIException(AIErrorCode.RateLimited, $"AzureOpenAI: {msg}", "AzureOpenAI", isRetryable: true),
            >= HttpStatusCode.InternalServerError =>
                new AIException(AIErrorCode.ProviderUnavailable, $"AzureOpenAI server error: {msg}", "AzureOpenAI", isRetryable: true),
            _ => new AIException(AIErrorCode.InvalidRequest, $"AzureOpenAI: {msg}", "AzureOpenAI")
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

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class AzureChatResponse
    {
        public string? Model { get; set; }
        public List<AzureChoice>? Choices { get; set; }
        public AzureUsage? Usage { get; set; }
    }

    private sealed class AzureChoice
    {
        public AzureMessage? Message { get; set; }
    }

    private sealed class AzureMessage
    {
        public string? Content { get; set; }
    }

    private sealed class AzureUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
