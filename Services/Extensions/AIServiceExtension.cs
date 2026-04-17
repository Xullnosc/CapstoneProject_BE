using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.AI.Caching;
using Services.AI.Configuration;
using Services.AI.Monitoring;
using Services.AI.Providers;
using Services.AI.RateLimiting;
using Services.AI.Text;

namespace Services.Extensions;

/// <summary>
/// Registers all AI services into the DI container.
/// Call <c>builder.Services.AddAIServices(builder.Configuration)</c> in Program.cs.
/// </summary>
public static class AIServiceExtension
{
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string overrideFilePath = "ai-settings-override.json")
    {
        // Bind configuration
        services.Configure<AIConfig>(configuration.GetSection(AIConfig.SectionKey));

        // Settings management service
        services.AddSingleton<IAISettingsService>(sp => new AISettingsService(
            sp.GetRequiredService<IOptionsMonitor<AIConfig>>(),
            overrideFilePath,
            sp.GetRequiredService<ILogger<AISettingsService>>()));
        services.AddScoped<IUserAISettingsService, UserAISettingsService>();

        // Singleton — safe to share across requests; no mutable state except thread-safe metrics
        services.AddSingleton<IAIMetricsCollector, AIMetricsCollector>();

        // Scoped — provider factory is cheap, but contains a lazy cache so scoped is cleaner
        services.AddScoped<AIProviderFactory>();
        services.AddScoped<IAICache, RedisAICache>();
        services.AddScoped<IAIRateLimiter, RedisRateLimiter>();
        services.AddScoped<IAIService, AIService>();
        services.AddSingleton<ITfIdfService, TfIdfService>();
        services.AddSingleton<IHybridChunkingService, HybridChunkingService>();
        services.AddSingleton<IChunkPreFilterService, ChunkPreFilterService>();

        return services;
    }
}
