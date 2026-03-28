using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.AI.Configuration;

/// <summary>
/// Reads the current AI configuration from <see cref="IOptionsMonitor{T}"/> and persists
/// admin-submitted overrides to <c>ai-settings-override.json</c>, which is registered as a
/// hot-reload configuration source in Program.cs so changes are picked up without restart.
/// </summary>
public sealed class AISettingsService : IAISettingsService
{
    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null // PascalCase to match appsettings convention
    };

    private readonly IOptionsMonitor<AIConfig> _configMonitor;
    private readonly string _overrideFilePath;
    private readonly ILogger<AISettingsService> _logger;

    public AISettingsService(
        IOptionsMonitor<AIConfig> configMonitor,
        string overrideFilePath,
        ILogger<AISettingsService> logger)
    {
        _configMonitor = configMonitor;
        _overrideFilePath = overrideFilePath;
        _logger = logger;
    }

    public AIConfigViewDto GetCurrentConfig()
    {
        var cfg = _configMonitor.CurrentValue;

        return new AIConfigViewDto
        {
            Enabled = cfg.Enabled,
            DefaultProvider = cfg.DefaultProvider.ToString(),
            FallbackProvider = cfg.FallbackProvider?.ToString(),
            Providers = cfg.Providers.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => MapProviderToView(kvp.Value)),
            Cache = new CacheViewDto
            {
                Enabled = cfg.Cache.Enabled,
                DefaultTtlSeconds = cfg.Cache.DefaultTtlSeconds
            },
            RateLimit = new RateLimitViewDto
            {
                RequestsPerMinutePerUser = cfg.RateLimit.RequestsPerMinutePerUser,
                RequestsPerMinuteGlobal = cfg.RateLimit.RequestsPerMinuteGlobal
            }
        };
    }

    public async Task SaveConfigAsync(UpdateAIConfigRequest request, CancellationToken cancellationToken = default)
    {
        var currentCfg = _configMonitor.CurrentValue;

        // Merge incoming request with existing config (keep existing API keys when blank)
        var providers = new Dictionary<string, object>();
        foreach (var (providerName, incoming) in request.Providers)
        {
            if (!Enum.TryParse<AIProviderType>(providerName, ignoreCase: true, out var providerType))
                continue;

            // Resolve API key: keep existing if the incoming is blank (masked placeholder)
            var existingKey = currentCfg.Providers.TryGetValue(providerType, out var existing)
                ? existing.ApiKey
                : string.Empty;

            var resolvedKey = string.IsNullOrWhiteSpace(incoming.ApiKey) || incoming.ApiKey.StartsWith('*')
                ? existingKey
                : incoming.ApiKey;

            providers[providerName] = new
            {
                ApiKey = resolvedKey,
                incoming.Model,
                incoming.BaseUrl,
                incoming.DeploymentName,
                incoming.TimeoutSeconds,
                incoming.MaxRetries
            };
        }

        var overrideDoc = new
        {
            AI = new
            {
                request.Enabled,
                request.DefaultProvider,
                request.FallbackProvider,
                Providers = providers,
                Cache = new
                {
                    request.Cache.Enabled,
                    request.Cache.DefaultTtlSeconds
                },
                RateLimit = new
                {
                    request.RateLimit.RequestsPerMinutePerUser,
                    request.RateLimit.RequestsPerMinuteGlobal
                }
            }
        };

        var json = JsonSerializer.Serialize(overrideDoc, _writeOptions);
        var dir = Path.GetDirectoryName(_overrideFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(_overrideFilePath, json, cancellationToken);
        _logger.LogInformation("AI settings override written to {Path}.", _overrideFilePath);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ProviderViewDto MapProviderToView(ProviderSettings settings)
    {
        var hasKey = !string.IsNullOrWhiteSpace(settings.ApiKey);
        return new ProviderViewDto
        {
            HasApiKey = hasKey,
            ApiKeyMasked = hasKey ? MaskApiKey(settings.ApiKey) : string.Empty,
            Model = settings.Model,
            BaseUrl = settings.BaseUrl,
            DeploymentName = settings.DeploymentName,
            TimeoutSeconds = settings.TimeoutSeconds,
            MaxRetries = settings.MaxRetries
        };
    }

    private static string MaskApiKey(string key)
    {
        if (key.Length <= 8)
            return new string('*', key.Length);

        return string.Concat(key.AsSpan(0, 4), new string('*', key.Length - 8), key.AsSpan(key.Length - 4));
    }
}
