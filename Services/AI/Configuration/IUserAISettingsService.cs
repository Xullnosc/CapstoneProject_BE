namespace Services.AI.Configuration;

public interface IUserAISettingsService
{
    Task<UserAISettingsViewDto> GetSettingsAsync(
        int userId,
        CancellationToken cancellationToken = default
    );
    Task SaveSettingsAsync(
        int userId,
        SaveUserAISettingsRequest request,
        CancellationToken cancellationToken = default
    );
    Task DeleteProviderAsync(
        int userId,
        string entryKey,
        CancellationToken cancellationToken = default
    );
    Task<string> AddEntryAsync(
        int userId,
        SaveUserAIProviderDto dto,
        CancellationToken cancellationToken = default
    );
    Task SetDefaultEntryAsync(
        int userId,
        string entryKey,
        CancellationToken cancellationToken = default
    );
    Task<UserAIExecutionSettingsDto?> GetEffectiveProviderSettingsAsync(
        int userId,
        CancellationToken cancellationToken = default
    );
}

public sealed class UserAIExecutionSettingsDto
{
    public string Provider { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}

public sealed class UserAISettingsViewDto
{
    public bool AiEnabled { get; init; }

    /// <summary>Provider name of the currently active entry (e.g. "OpenAI"). For backward compat.</summary>
    public string DefaultProvider { get; init; } = string.Empty;

    /// <summary>Entry key of the currently active entry (dict key in storage).</summary>
    public string DefaultEntryKey { get; init; } = string.Empty;
    public List<UserAIProviderViewDto> Providers { get; init; } = new();
}

public sealed class UserAIProviderViewDto
{
    /// <summary>Unique storage key for this entry (the dict key in Redis).</summary>
    public string EntryKey { get; init; } = string.Empty;

    /// <summary>Provider type name, e.g. "OpenAI", "GoogleGemini".</summary>
    public string Provider { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public bool HasApiKey { get; init; }
    public string ApiKeyMasked { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
}

public sealed class SaveUserAISettingsRequest
{
    public string DefaultProvider { get; init; } = string.Empty;
    public List<SaveUserAIProviderDto> Providers { get; init; } = new();
}

public sealed class SaveUserAIProviderDto
{
    /// <summary>When provided, updates the specific entry with this key. When null, falls back to using Provider name as key (legacy/settings-page flow).</summary>
    public string? EntryKey { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}

internal sealed class UserAISettingsStore
{
    public string DefaultProvider { get; init; } = string.Empty;
    public Dictionary<string, UserAIProviderStore> Providers { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class UserAIProviderStore
{
    /// <summary>Explicit provider name (e.g. "OpenAI"). Empty for legacy entries where the dict key was the provider name.</summary>
    public string Provider { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}
