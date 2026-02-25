using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> SetValueAsync(
        string key,
        string value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Expiration exp = expiry.HasValue ? new Expiration(expiry.Value) : default;
            var result = await _database.StringSetAsync(
                key,
                value,
                exp
            );

            _logger.LogInformation("Redis SET {Key} (Expiry: {Expiry})", key, expiry);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SET failed for key {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogDebug("Redis MISS {Key}", key);
                return null;
            }

            _logger.LogDebug("Redis HIT {Key}", key);
            return value!;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis GET failed for key {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            _logger.LogInformation("Redis DELETE {Key}", key);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis DELETE failed for key {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetObjectAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        return await SetValueAsync(key, json, expiry, cancellationToken);
    }

    public async Task<T?> GetObjectAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var json = await GetValueAsync(key, cancellationToken);
        return json is null
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }

    private static bool IsRedisException(Exception ex)
        => ex is RedisConnectionException
        || ex is RedisTimeoutException
        || ex is RedisServerException;
}