using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.AI.Configuration;
using BusinessObjects.AI.Models;
using Services.AI.Providers.Implementations;

namespace Services.AI.Providers;

/// <summary>
/// Resolves the appropriate <see cref="IAIProvider"/> for a given <see cref="AIProviderType"/>.
/// Providers are instantiated lazily and cached for the factory's lifetime.
/// </summary>
internal sealed class AIProviderFactory
{
    private readonly IOptionsMonitor<AIConfig> _configMonitor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIProviderFactory> _logger;

    public AIProviderFactory(
        IOptionsMonitor<AIConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<AIProviderFactory> logger)
    {
        _configMonitor = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IAIProvider GetProvider(AIProviderType type, AIRequest? request = null)
    {
        var config = _configMonitor.CurrentValue;
        var settings = ResolveSettings(type, config, request);

        var provider = type switch
        {
            AIProviderType.OpenAI      => (IAIProvider)new OpenAIProvider(settings, _httpClientFactory),
            AIProviderType.AzureOpenAI => new AzureOpenAIProvider(settings, _httpClientFactory),
            AIProviderType.Anthropic   => new AnthropicProvider(settings, _httpClientFactory),
            AIProviderType.GoogleGemini => new GoogleGeminiProvider(settings, _httpClientFactory),
            _ => throw new AIException(AIErrorCode.NotConfigured,
                    $"Unsupported AI provider type: {type}", type.ToString())
        };

        return provider;
    }

    public IAIProvider GetDefault(AIRequest? request = null)
    {
        var config = _configMonitor.CurrentValue;
        if (!string.IsNullOrWhiteSpace(request?.Provider)
            && Enum.TryParse<AIProviderType>(request.Provider, ignoreCase: true, out var requestProvider))
            return GetProvider(requestProvider, request);

        return GetProvider(config.DefaultProvider, request);
    }

    /// <summary>Returns the configured fallback provider, or null if none is configured.</summary>
    public IAIProvider? TryGetFallback(AIRequest? request = null)
    {
        var config = _configMonitor.CurrentValue;
        if (config.FallbackProvider is null)
            return null;

        try
        {
            return GetProvider(config.FallbackProvider.Value, request);
        }
        catch (AIException ex)
        {
            _logger.LogWarning("Fallback provider '{Provider}' is not available: {Message}",
                config.FallbackProvider, ex.Message);
            return null;
        }
    }

    private static ProviderSettings ResolveSettings(AIProviderType type, AIConfig config, AIRequest? request)
    {
        config.Providers.TryGetValue(type, out var configuredSettings);

        var requestSettings = request?.ProviderSettings;
        if (requestSettings is null)
        {
            if (configuredSettings is null)
                throw new AIException(AIErrorCode.NotConfigured,
                    $"Provider '{type}' is not present in AI:Providers configuration.", type.ToString());

            return configuredSettings;
        }

        return new ProviderSettings
        {
            ApiKey = string.IsNullOrWhiteSpace(requestSettings.ApiKey)
                ? configuredSettings?.ApiKey ?? string.Empty
                : requestSettings.ApiKey,
            Model = string.IsNullOrWhiteSpace(requestSettings.Model)
                ? configuredSettings?.Model ?? string.Empty
                : requestSettings.Model,
            BaseUrl = requestSettings.BaseUrl ?? configuredSettings?.BaseUrl,
            ApiVersion = requestSettings.ApiVersion ?? configuredSettings?.ApiVersion,
            DeploymentName = requestSettings.DeploymentName ?? configuredSettings?.DeploymentName,
            TimeoutSeconds = requestSettings.TimeoutSeconds ?? configuredSettings?.TimeoutSeconds ?? 60,
            MaxRetries = requestSettings.MaxRetries ?? configuredSettings?.MaxRetries ?? 2,
        };
    }
}
