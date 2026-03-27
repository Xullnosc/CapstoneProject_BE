using BusinessObjects;
using BusinessObjects.AI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.AI.Configuration;
using System.Security.Claims;

namespace CapstoneProject_BE.Controllers;

/// <summary>
/// AI API surface.
///
/// All endpoints require authentication. Rate limiting and caching are handled
/// internally by <see cref="IAIService"/>.
///
/// Extend this controller (or create domain-specific controllers) to expose
/// use-case-specific AI features (e.g. thesis analysis, advisor matching, etc.).
/// </summary>
[Route("api/ai")]
[ApiController]
[Authorize]
public sealed class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IUserAISettingsService _userAiSettings;
    private readonly ILogger<AIController> _logger;

    public AIController(IAIService aiService, IUserAISettingsService userAiSettings, ILogger<AIController> logger)
    {
        _aiService = aiService;
        _userAiSettings = userAiSettings;
        _logger = logger;
    }

    // ─── Health / status ──────────────────────────────────────────────────────

    /// <summary>Returns whether AI features are enabled and the service is reachable.</summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { enabled = _aiService.IsEnabled });
    }

    // ─── Core chat endpoint ───────────────────────────────────────────────────

    /// <summary>
    /// General-purpose AI chat endpoint.
    /// Accepts a conversation and returns the assistant's reply.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] AIChatRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!_aiService.IsEnabled)
            return StatusCode(503, new { message = "AI features are currently disabled." });

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = new AIRequest
            {
                Messages = dto.Messages.Select(m => new AIMessage(
                    m.Role.ToLower() switch
                    {
                        "system"    => AIMessageRole.System,
                        "assistant" => AIMessageRole.Assistant,
                        _           => AIMessageRole.User
                    },
                    m.Content)).ToArray(),
                SystemPrompt = dto.SystemPrompt,
                Temperature = dto.Temperature ?? 0.7f,
                MaxTokens = dto.MaxTokens ?? 1024,
                UseCache = dto.UseCache ?? true,
                UserId = userId,
                Provider = dto.Provider,
                ProviderSettings = dto.ProviderSettings is null
                    ? null
                    : new AIProviderRequestSettings
                    {
                        ApiKey = dto.ProviderSettings.ApiKey,
                        Model = dto.ProviderSettings.Model,
                        BaseUrl = dto.ProviderSettings.BaseUrl,
                        ApiVersion = dto.ProviderSettings.ApiVersion,
                        DeploymentName = dto.ProviderSettings.DeploymentName,
                        TimeoutSeconds = dto.ProviderSettings.TimeoutSeconds,
                        MaxRetries = dto.ProviderSettings.MaxRetries,
                    }
            };

            var response = await _aiService.ChatAsync(request, cancellationToken);

            return Ok(new
            {
                content = response.Content,
                provider = response.Provider,
                model = response.Model,
                fromCache = response.FromCache,
                latencyMs = (int)response.Latency.TotalMilliseconds,
                usage = new
                {
                    promptTokens = response.Usage.PromptTokens,
                    completionTokens = response.Usage.CompletionTokens,
                    totalTokens = response.Usage.TotalTokens,
                    estimatedCostUsd = response.Usage.EstimatedCostUsd
                }
            });
        }
        catch (AIException ex)
        {
            _logger.LogWarning("AI chat request rejected: {Code} — {Message}", ex.Code, ex.Message);
            return MapAIException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AI chat endpoint.");
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    // ─── Metrics (Admin only) ─────────────────────────────────────────────────

    /// <summary>Returns cumulative AI call metrics. Restricted to Admin role.</summary>
    [HttpGet("metrics")]
    [Authorize(Roles = CampusConstants.Roles.Admin)]
    public IActionResult GetMetrics()
    {
        var summary = _aiService.GetMetrics();
        return Ok(summary);
    }

    // ─── User BYOK settings ───────────────────────────────────────────────────

    [HttpGet("user-settings")]
    public async Task<IActionResult> GetUserSettings(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "User identity is invalid." });

        var settings = await _userAiSettings.GetSettingsAsync(userId, cancellationToken);
        return Ok(settings);
    }

    [HttpPut("user-settings")]
    public async Task<IActionResult> SaveUserSettings(
        [FromBody] SaveUserAISettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "User identity is invalid." });

        try
        {
            await _userAiSettings.SaveSettingsAsync(userId, request, cancellationToken);
            return Ok(new { message = "Your AI provider settings were saved to Redis." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user AI settings for user {UserId}.", userId);
            return StatusCode(500, new { message = "Failed to save your AI provider settings." });
        }
    }

    [HttpDelete("user-settings/{provider}")]
    public async Task<IActionResult> DeleteUserProvider(string provider, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "User identity is invalid." });

        await _userAiSettings.DeleteProviderAsync(userId, provider, cancellationToken);
        return NoContent();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private IActionResult MapAIException(AIException ex) => ex.Code switch
    {
        AIErrorCode.Disabled          => StatusCode(503, new { message = ex.Message, code = ex.Code.ToString() }),
        AIErrorCode.RateLimited       => StatusCode(429, new { message = ex.Message, code = ex.Code.ToString(), retryAfterSeconds = 60 }),
        AIErrorCode.InvalidApiKey     => StatusCode(502, new { message = "AI provider configuration error. Please contact an administrator.", code = ex.Code.ToString() }),
        AIErrorCode.QuotaExceeded     => StatusCode(402, new { message = "AI provider quota exceeded. Please contact an administrator.", code = ex.Code.ToString() }),
        AIErrorCode.ContentFiltered   => StatusCode(400, new { message = ex.Message, code = ex.Code.ToString() }),
        AIErrorCode.InvalidRequest    => StatusCode(400, new { message = ex.Message, code = ex.Code.ToString() }),
        AIErrorCode.Timeout           => StatusCode(504, new { message = "AI provider request timed out. Please try again.", code = ex.Code.ToString() }),
        AIErrorCode.ProviderUnavailable => StatusCode(503, new { message = "AI provider is temporarily unavailable.", code = ex.Code.ToString() }),
        _                             => StatusCode(500, new { message = "An unexpected AI error occurred.", code = ex.Code.ToString() })
    };

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class AIChatRequestDto
{
    public required List<AIChatMessageDto> Messages { get; init; }
    public string? SystemPrompt { get; init; }
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public bool? UseCache { get; init; }
    public string? Provider { get; init; }
    public AIProviderSettingsDto? ProviderSettings { get; init; }
}

public sealed class AIChatMessageDto
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

public sealed class AIProviderSettingsDto
{
    public string ApiKey { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int? TimeoutSeconds { get; init; }
    public int? MaxRetries { get; init; }
}
