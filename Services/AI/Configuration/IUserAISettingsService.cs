namespace Services.AI.Configuration;

public interface IUserAISettingsService
{
    Task<UserAISettingsViewDto> GetSettingsAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(int userId, SaveUserAISettingsRequest request, CancellationToken cancellationToken = default);
    Task DeleteProviderAsync(int userId, string provider, CancellationToken cancellationToken = default);
}

public sealed class UserAISettingsViewDto
{
    public bool AiEnabled { get; init; }
    public string DefaultProvider { get; init; } = string.Empty;
    public List<UserAIProviderViewDto> Providers { get; init; } = new();
}

public sealed class UserAIProviderViewDto
{
    public string Provider { get; init; } = string.Empty;
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
    public string Provider { get; init; } = string.Empty;
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
    public Dictionary<string, UserAIProviderStore> Providers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class UserAIProviderStore
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}