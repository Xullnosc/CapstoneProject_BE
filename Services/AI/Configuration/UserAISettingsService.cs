using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.AI.Configuration;

public sealed class UserAISettingsService : IUserAISettingsService
{
    private static readonly TimeSpan SettingsTtl = TimeSpan.FromDays(30);

    private readonly IRedisService _redisService;
    private readonly IOptionsMonitor<AIConfig> _configMonitor;
    private readonly ILogger<UserAISettingsService> _logger;

    public UserAISettingsService(
        IRedisService redisService,
        IOptionsMonitor<AIConfig> configMonitor,
        ILogger<UserAISettingsService> logger)
    {
        _redisService = redisService;
        _configMonitor = configMonitor;
        _logger = logger;
    }

    public async Task<UserAISettingsViewDto> GetSettingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var config = _configMonitor.CurrentValue;
        var store = await GetStoreAsync(userId, cancellationToken) ?? new UserAISettingsStore
        {
            DefaultProvider = config.DefaultProvider.ToString()
        };

        return new UserAISettingsViewDto
        {
            AiEnabled = config.Enabled,
            DefaultProvider = string.IsNullOrWhiteSpace(store.DefaultProvider)
                ? config.DefaultProvider.ToString()
                : store.DefaultProvider,
            Providers = store.Providers
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new UserAIProviderViewDto
                {
                    Provider = kvp.Key,
                    HasApiKey = !string.IsNullOrWhiteSpace(kvp.Value.ApiKey),
                    ApiKeyMasked = MaskKey(kvp.Value.ApiKey),
                    Model = kvp.Value.Model,
                    BaseUrl = kvp.Value.BaseUrl,
                    ApiVersion = kvp.Value.ApiVersion,
                    DeploymentName = kvp.Value.DeploymentName,
                    TimeoutSeconds = kvp.Value.TimeoutSeconds,
                    MaxRetries = kvp.Value.MaxRetries,
                })
                .ToList()
        };
    }

    public async Task SaveSettingsAsync(int userId, SaveUserAISettingsRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await GetStoreAsync(userId, cancellationToken) ?? new UserAISettingsStore();
        var providers = new Dictionary<string, UserAIProviderStore>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in request.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Provider))
                continue;

            existing.Providers.TryGetValue(provider.Provider, out var current);
            var resolvedKey = string.IsNullOrWhiteSpace(provider.ApiKey)
                ? current?.ApiKey ?? string.Empty
                : provider.ApiKey;

            providers[provider.Provider] = new UserAIProviderStore
            {
                ApiKey = resolvedKey,
                Model = provider.Model,
                BaseUrl = provider.BaseUrl,
                ApiVersion = provider.ApiVersion,
                DeploymentName = provider.DeploymentName,
                TimeoutSeconds = provider.TimeoutSeconds,
                MaxRetries = provider.MaxRetries,
            };
        }

        var store = new UserAISettingsStore
        {
            DefaultProvider = request.DefaultProvider,
            Providers = providers,
        };

        await _redisService.SetObjectAsync(GetRedisKey(userId), store, SettingsTtl, cancellationToken);
        _logger.LogInformation("Saved AI settings to Redis for user {UserId} with {ProviderCount} providers.", userId, providers.Count);
    }

    public async Task DeleteProviderAsync(int userId, string provider, CancellationToken cancellationToken = default)
    {
        var store = await GetStoreAsync(userId, cancellationToken);
        if (store is null)
            return;

        if (!store.Providers.Remove(provider))
            return;

        await _redisService.SetObjectAsync(GetRedisKey(userId), store, SettingsTtl, cancellationToken);
        _logger.LogInformation("Deleted AI provider {Provider} from Redis settings for user {UserId}.", provider, userId);
    }

    private Task<UserAISettingsStore?> GetStoreAsync(int userId, CancellationToken cancellationToken)
        => _redisService.GetObjectAsync<UserAISettingsStore>(GetRedisKey(userId), cancellationToken);

    private static string GetRedisKey(int userId) => $"ai:user-settings:{userId}";

    private static string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (key.Length <= 8)
            return new string('*', key.Length);

        return string.Concat(key.AsSpan(0, 4), new string('*', key.Length - 8), key.AsSpan(key.Length - 4));
    }
}