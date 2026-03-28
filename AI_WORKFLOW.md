# AI Workflow Documentation

## 1. Purpose and Scope
This document explains how AI requests move through the backend, from API entry to provider response, including validation, rate limiting, caching, retries, fallback, metrics, and user BYOK settings.

Primary API surface is in `CapstoneProject_BE/Controllers/AIController.cs`.

## 2. High-Level Architecture
Core components:

- Controller: receives authenticated HTTP requests and maps DTOs to domain models.
- Service orchestrator: executes the pipeline for validation, throttling, cache, provider call, and metrics.
- Provider adapters: translate unified requests into provider-specific HTTP payloads.
- Redis cache and rate limiter: optimize latency/cost and enforce quotas.
- Metrics collector: records operational usage and performance.

Key files:

- `CapstoneProject_BE/Controllers/AIController.cs`
- `Services/AIService.cs`
- `Services/AI/Providers/AIProviderFactory.cs`
- `Services/AI/Validation/PromptValidator.cs`
- `Services/AI/Caching/RedisAICache.cs`
- `Services/AI/RateLimiting/RedisRateLimiter.cs`
- `Services/AI/Monitoring/AIMetricsCollector.cs`

## 3. End-to-End Request Lifecycle (Chat)
Entry endpoint: `POST /api/ai/chat`

### Step 1: Request Mapping in Controller
`AIController.Chat` converts `AIChatRequestDto` into `AIRequest`:

- Maps role strings (`system`, `assistant`, `user`) to `AIMessageRole`.
- Applies defaults when absent:
  - `Temperature = 0.7`
  - `MaxTokens = 1024`
  - `UseCache = true`
- Adds current authenticated user id for per-user throttling.
- Supports per-request provider override and BYOK provider settings.

### Step 2: Feature Flag Gate
`AIService.ChatAsync` first checks `AIConfig.Enabled`.

- If disabled, throws `AIException(Disabled)`.
- Controller maps this to HTTP 503.

### Step 3: Prompt Validation and Content Safety
`PromptValidator.Validate` enforces:

- Non-empty message list.
- At least one `User` message.
- Temperature in [0, 2].
- MaxTokens in [1, 128000].
- Per-message character cap (16000).
- Total character cap (32000).
- Prompt injection screening with case-insensitive regex patterns for suspicious control phrases.

On failure, throws:

- `InvalidRequest` for malformed/oversized input.
- `ContentFiltered` for suspicious prompt patterns.

### Step 4: Rate Limiting
`RedisRateLimiter.CheckAsync(userId)` applies a sliding 1-minute window:

- Global limit via `ai:rl:global`.
- Per-user limit via `ai:rl:user:{userId}`.

Behavior details:

- Limits come from `AIConfig.RateLimit`.
- If Redis is unavailable, request is allowed (availability-first fail-open policy).
- If exceeded, service throws `AIException(RateLimited)` and controller returns HTTP 429.

### Step 5: Provider Resolution
`AIProviderFactory.GetDefault(request)` decides provider in this order:

1. Request-level provider override (if valid).
2. Configured `AIConfig.DefaultProvider`.

Provider settings are merged from:

- Server config (`AIConfig.Providers`) and
- Request-level BYOK settings (`AIRequest.ProviderSettings`) when supplied.

### Step 6: Cache Lookup
If both config cache and request cache are enabled:

- Build deterministic cache key from provider/model/messages/temperature/maxTokens.
- Read Redis cache.
- If hit, deserialize `AIResponse`, set `FromCache = true`, return immediately.

### Step 7: Provider Call with Retry and Fallback
`CallWithRetryAndFallbackAsync` executes:

- Calls selected provider adapter.
- Retries only retryable failures with exponential backoff:
  - 0.5s, 1s, 2s (based on attempt index and provider max retry setting).
- On non-retryable failure, attempts configured fallback provider (if present and different).

### Step 8: Cache Write
On successful non-cached response:

- Serialize response to JSON.
- Store using TTL:
  - request override `CacheTtlSeconds`, else
  - `AIConfig.Cache.DefaultTtlSeconds`.

### Step 9: Metrics Recording
For each path (success, failure, cache hit), metrics collector tracks:

- Total/success/failed call counts.
- Cache hit count.
- Latency accumulation.
- Token totals.
- Estimated cost totals.
- Per-provider call distribution.

## 4. Provider Adapter Model
All providers implement `IAIProvider`:

- `OpenAIProvider`
- `AzureOpenAIProvider`
- `AnthropicProvider`
- `GoogleGeminiProvider`

Responsibilities per adapter:

- Build provider-specific HTTP payload from `AIRequest`.
- Execute API call with timeout.
- Normalize errors to `AIException` + `AIErrorCode`.
- Return unified `AIResponse` with usage and latency.

## 5. Error Taxonomy and HTTP Mapping
Primary domain error codes:

- `Disabled`
- `RateLimited`
- `InvalidRequest`
- `ContentFiltered`
- `InvalidApiKey`
- `QuotaExceeded`
- `Timeout`
- `ProviderUnavailable`
- `NotConfigured`
- `Unknown`

Controller response mapping (`MapAIException`):

- 503: Disabled, ProviderUnavailable
- 429: RateLimited
- 400: InvalidRequest, ContentFiltered
- 502: InvalidApiKey (masked provider configuration issue)
- 402: QuotaExceeded
- 504: Timeout
- 500: default/unknown

## 6. Configuration Model
`AIConfig` controls runtime behavior:

- `Enabled`
- `DefaultProvider`
- `FallbackProvider`
- `Providers[AIProviderType]`:
  - ApiKey
  - Model
  - BaseUrl
  - ApiVersion
  - DeploymentName
  - TimeoutSeconds
  - MaxRetries
- `Cache`:
  - Enabled
  - DefaultTtlSeconds
- `RateLimit`:
  - RequestsPerMinutePerUser
  - RequestsPerMinuteGlobal

## 7. BYOK User Settings Workflow
User-specific provider settings are persisted in Redis through `UserAISettingsService`:

- `GET /api/ai/user-settings`: returns masked API key states and provider config.
- `PUT /api/ai/user-settings`: saves per-user provider config; blank key preserves existing key.
- `DELETE /api/ai/user-settings/{provider}`: removes one provider config.

Storage notes:

- Key namespace derived from user id.
- TTL defaults to 30 days.
- Supports user-level override without changing server-level config.

## 8. Observability and Operational Notes
Metrics are in-memory (`AIMetricsCollector`):

- Useful for dashboard summaries and quick operational visibility.
- Resets on service restart.
- For long-term billing/audit, replace or complement with persistent storage.

Logging behavior:

- Retry and fallback events are logged with provider and attempt context.
- Cache miss/hit/set logged at debug level.
- Redis failures for cache/rate limit are logged with graceful degradation.

## 9. Sequence (Text)

1. Client sends `POST /api/ai/chat`.
2. Controller maps DTO to `AIRequest`.
3. Service checks `AIConfig.Enabled`.
4. Service validates request (`PromptValidator`).
5. Service enforces rate limit (`RedisRateLimiter`).
6. Service resolves provider (`AIProviderFactory`).
7. Service checks cache (`RedisAICache`).
8. On miss, service calls provider with retry/fallback.
9. Service writes cache for successful response.
10. Service records metrics.
11. Controller returns normalized response payload.

## 10. Security and Reliability Characteristics
Security controls:

- Authentication required on AI endpoints.
- Prompt pattern filtering for suspicious instruction takeover attempts.
- Secrets are masked in returned settings views.

Reliability controls:

- Timeouts and retry on transient provider errors.
- Optional fallback provider.
- Cache and rate limit degradation paths avoid total outage when Redis has transient faults.

## 11. Relevant Source Index

- API controller: `CapstoneProject_BE/Controllers/AIController.cs`
- Orchestrator: `Services/AIService.cs`
- Public service contract: `Services/IAIService.cs`
- Config types: `Services/AI/Configuration/AIConfig.cs`
- Runtime settings service: `Services/AI/Configuration/AISettingsService.cs`
- User settings service: `Services/AI/Configuration/UserAISettingsService.cs`
- Prompt validator: `Services/AI/Validation/PromptValidator.cs`
- Cache abstraction/impl: `Services/AI/Caching/IAICache.cs`, `Services/AI/Caching/RedisAICache.cs`
- Rate limiting abstraction/impl: `Services/AI/RateLimiting/IAIRateLimiter.cs`, `Services/AI/RateLimiting/RedisRateLimiter.cs`
- Provider contract/factory: `Services/AI/Providers/IAIProvider.cs`, `Services/AI/Providers/AIProviderFactory.cs`
- Provider adapters:
  - `Services/AI/Providers/Implementations/OpenAIProvider.cs`
  - `Services/AI/Providers/Implementations/AzureOpenAIProvider.cs`
  - `Services/AI/Providers/Implementations/AnthropicProvider.cs`
  - `Services/AI/Providers/Implementations/GoogleGeminiProvider.cs`
- Domain models: `BusinessObjects/AI/Models/*`
- Metrics collector: `Services/AI/Monitoring/AIMetricsCollector.cs`
- Tests:
  - `FCTMS.Tests/Services/AI/AIServiceTests.cs`
  - `FCTMS.Tests/Services/AI/AIMetricsCollectorTests.cs`
  - `FCTMS.Tests/Services/AI/PromptValidatorTests.cs`
