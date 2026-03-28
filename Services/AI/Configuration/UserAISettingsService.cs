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
        ILogger<UserAISettingsService> logger
    )
    {
        _redisService = redisService;
        _configMonitor = configMonitor;
        _logger = logger;
    }

    public async Task<UserAISettingsViewDto> GetSettingsAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var config = _configMonitor.CurrentValue;
        var store =
            await GetStoreAsync(userId, cancellationToken)
            ?? new UserAISettingsStore { DefaultProvider = config.DefaultProvider.ToString() };

        var defaultEntryKey = string.IsNullOrWhiteSpace(store.DefaultProvider)
            ? config.DefaultProvider.ToString()
            : store.DefaultProvider;

        // Derive the human-readable default provider name from the default entry
        var defaultProviderName = store.Providers.TryGetValue(defaultEntryKey, out var defaultEntry)
            ? GetProviderName(defaultEntryKey, defaultEntry)
            : defaultEntryKey; // fallback: use the key itself (legacy behaviour)

        return new UserAISettingsViewDto
        {
            AiEnabled = config.Enabled,
            DefaultProvider = defaultProviderName,
            DefaultEntryKey = defaultEntryKey,
            Providers = store
                .Providers.OrderBy(kvp => kvp.Key)
                .Select(kvp => new UserAIProviderViewDto
                {
                    EntryKey = kvp.Key,
                    Provider = GetProviderName(kvp.Key, kvp.Value),
                    Nickname = kvp.Value.Nickname,
                    HasApiKey = !string.IsNullOrWhiteSpace(kvp.Value.ApiKey),
                    ApiKeyMasked = MaskKey(kvp.Value.ApiKey),
                    Model = kvp.Value.Model,
                    BaseUrl = kvp.Value.BaseUrl,
                    ApiVersion = kvp.Value.ApiVersion,
                    DeploymentName = kvp.Value.DeploymentName,
                    TimeoutSeconds = kvp.Value.TimeoutSeconds,
                    MaxRetries = kvp.Value.MaxRetries,
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Merges the request entries into the existing store (preserves entries not mentioned in the request).
    /// When a <see cref="SaveUserAIProviderDto.EntryKey"/> is supplied the specific entry is updated;
    /// otherwise the provider name is used as the dict key (legacy/settings-page behaviour).
    /// </summary>
    public async Task SaveSettingsAsync(
        int userId,
        SaveUserAISettingsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await GetStoreAsync(userId, cancellationToken) ?? new UserAISettingsStore();

        // Start from the existing entries so unmentioned entries are preserved.
        var providers = new Dictionary<string, UserAIProviderStore>(
            existing.Providers,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var dto in request.Providers)
        {
            if (string.IsNullOrWhiteSpace(dto.Provider))
                continue;

            var dictKey = string.IsNullOrWhiteSpace(dto.EntryKey) ? dto.Provider : dto.EntryKey;
            existing.Providers.TryGetValue(dictKey, out var current);
            var resolvedKey = string.IsNullOrWhiteSpace(dto.ApiKey)
                ? current?.ApiKey ?? string.Empty
                : dto.ApiKey;

            providers[dictKey] = new UserAIProviderStore
            {
                Provider = dto.Provider,
                Nickname = dto.Nickname ?? current?.Nickname ?? string.Empty,
                ApiKey = resolvedKey,
                Model = dto.Model,
                BaseUrl = dto.BaseUrl,
                ApiVersion = dto.ApiVersion,
                DeploymentName = dto.DeploymentName,
                TimeoutSeconds = dto.TimeoutSeconds,
                MaxRetries = dto.MaxRetries,
            };
        }

        var store = new UserAISettingsStore
        {
            DefaultProvider = request.DefaultProvider,
            Providers = providers,
        };

        await _redisService.SetObjectAsync(
            GetRedisKey(userId),
            store,
            SettingsTtl,
            cancellationToken
        );
        _logger.LogInformation(
            "Saved AI settings to Redis for user {UserId} with {ProviderCount} providers.",
            userId,
            providers.Count
        );
    }

    /// <summary>Adds a brand-new entry with an auto-generated key. Returns the generated entry key.</summary>
    public async Task<string> AddEntryAsync(
        int userId,
        SaveUserAIProviderDto dto,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dto.Provider))
            throw new ArgumentException("Provider is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.ApiKey))
            throw new ArgumentException("ApiKey is required when adding a new entry.", nameof(dto));

        var existing = await GetStoreAsync(userId, cancellationToken) ?? new UserAISettingsStore();
        var providers = new Dictionary<string, UserAIProviderStore>(
            existing.Providers,
            StringComparer.OrdinalIgnoreCase
        );

        // Generate a short opaque key that is URL-safe and guaranteed unique within the dict.
        string entryKey;
        do
        {
            entryKey = Guid.NewGuid().ToString("N")[..12];
        } while (providers.ContainsKey(entryKey));

        providers[entryKey] = new UserAIProviderStore
        {
            Provider = dto.Provider,
            Nickname = dto.Nickname ?? string.Empty,
            ApiKey = dto.ApiKey,
            Model = dto.Model,
            BaseUrl = dto.BaseUrl,
            ApiVersion = dto.ApiVersion,
            DeploymentName = dto.DeploymentName,
            TimeoutSeconds = dto.TimeoutSeconds,
            MaxRetries = dto.MaxRetries,
        };

        var store = new UserAISettingsStore
        {
            DefaultProvider = existing.DefaultProvider,
            Providers = providers,
        };

        await _redisService.SetObjectAsync(
            GetRedisKey(userId),
            store,
            SettingsTtl,
            cancellationToken
        );
        _logger.LogInformation(
            "Added AI entry {EntryKey} ({Provider}/{Model}) for user {UserId}.",
            entryKey,
            dto.Provider,
            dto.Model,
            userId
        );
        return entryKey;
    }

    /// <summary>Sets which entry is the active/default one for AI feature calls.</summary>
    public async Task SetDefaultEntryAsync(
        int userId,
        string entryKey,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await GetStoreAsync(userId, cancellationToken) ?? new UserAISettingsStore();

        if (!existing.Providers.ContainsKey(entryKey))
            throw new KeyNotFoundException(
                $"Entry key '{entryKey}' not found in the user's AI settings."
            );

        var store = new UserAISettingsStore
        {
            DefaultProvider = entryKey,
            Providers = existing.Providers,
        };

        await _redisService.SetObjectAsync(
            GetRedisKey(userId),
            store,
            SettingsTtl,
            cancellationToken
        );
        _logger.LogInformation(
            "Set default AI entry to {EntryKey} for user {UserId}.",
            entryKey,
            userId
        );
    }

    public async Task DeleteProviderAsync(
        int userId,
        string entryKey,
        CancellationToken cancellationToken = default
    )
    {
        var store = await GetStoreAsync(userId, cancellationToken);
        if (store is null)
            return;

        if (!store.Providers.Remove(entryKey))
            return;

        await _redisService.SetObjectAsync(
            GetRedisKey(userId),
            store,
            SettingsTtl,
            cancellationToken
        );
        _logger.LogInformation(
            "Deleted AI entry {EntryKey} from Redis settings for user {UserId}.",
            entryKey,
            userId
        );
    }

    public async Task<UserAIExecutionSettingsDto?> GetEffectiveProviderSettingsAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var config = _configMonitor.CurrentValue;
        var store = await GetStoreAsync(userId, cancellationToken);
        if (store is null || store.Providers.Count == 0)
            return null;

        var preferredKey = string.IsNullOrWhiteSpace(store.DefaultProvider)
            ? config.DefaultProvider.ToString()
            : store.DefaultProvider;

        if (
            store.Providers.TryGetValue(preferredKey, out var preferred)
            && !string.IsNullOrWhiteSpace(preferred.ApiKey)
        )
        {
            return MapExecutionSettings(preferredKey, preferred);
        }

        var fallback = store.Providers.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.Value.ApiKey)
        );
        if (string.IsNullOrWhiteSpace(fallback.Key))
            return null;

        return MapExecutionSettings(fallback.Key, fallback.Value);
    }

    private static UserAIExecutionSettingsDto MapExecutionSettings(
        string entryKey,
        UserAIProviderStore source
    )
    {
        return new UserAIExecutionSettingsDto
        {
            Provider = GetProviderName(entryKey, source),
            ApiKey = source.ApiKey,
            Model = source.Model,
            BaseUrl = source.BaseUrl,
            ApiVersion = source.ApiVersion,
            DeploymentName = source.DeploymentName,
            TimeoutSeconds = source.TimeoutSeconds,
            MaxRetries = source.MaxRetries,
        };
    }

    /// <summary>
    /// Returns the provider name for an entry.
    /// For new entries the <see cref="UserAIProviderStore.Provider"/> field is set explicitly.
    /// For legacy entries the dict key itself is the provider name.
    /// </summary>
    private static string GetProviderName(string entryKey, UserAIProviderStore store) =>
        string.IsNullOrEmpty(store.Provider) ? entryKey : store.Provider;

    private Task<UserAISettingsStore?> GetStoreAsync(
        int userId,
        CancellationToken cancellationToken
    ) => _redisService.GetObjectAsync<UserAISettingsStore>(GetRedisKey(userId), cancellationToken);

    private static string GetRedisKey(int userId) => $"ai:user-settings:{userId}";

    private static string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (key.Length <= 8)
            return new string('*', key.Length);

        return string.Concat(
            key.AsSpan(0, 4),
            new string('*', key.Length - 8),
            key.AsSpan(key.Length - 4)
        );
    }
}
