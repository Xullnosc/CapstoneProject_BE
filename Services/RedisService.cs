using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

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

    public async Task<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SetAddAsync(key, value);
            _logger.LogInformation("Redis SADD {Key} (Value: {Value}) Added: {Added}", key, value, result);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SADD failed for key {Key}", key);
            throw;
        }
    }

    public async Task<long> SetAddAsync(string key, IEnumerable<string> values, CancellationToken cancellationToken = default)
    {
        try
        {
            var rv = values.Select(v => (RedisValue)v).ToArray();
            var result = await _database.SetAddAsync(key, rv);
            _logger.LogInformation("Redis SADD {Key} (CountAttempted: {Count}) Added: {Added}", key, rv.Length, result);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SADD failed for key {Key}", key);
            throw;
        }
    }

    public async Task<string[]?> SetMembersAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _database.SetMembersAsync(key);
            if (members is null || members.Length == 0)
            {
                _logger.LogDebug("Redis SMEMBERS MISS {Key}", key);
                return Array.Empty<string>();
            }

            var result = members.Select(m => (string)m).ToArray();
            _logger.LogDebug("Redis SMEMBERS HIT {Key} Count: {Count}", key, result.Length);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SMEMBERS failed for key {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SetRemoveAsync(key, value);
            _logger.LogInformation("Redis SREM {Key} (Value: {Value}) Removed: {Removed}", key, value, result);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SREM failed for key {Key}", key);
            throw;
        }
    }

    public async Task<long> SetRemoveAsync(string key, IEnumerable<string> values, CancellationToken cancellationToken = default)
    {
        try
        {
            var rv = values.Select(v => (RedisValue)v).ToArray();
            var result = await _database.SetRemoveAsync(key, rv);
            _logger.LogInformation("Redis SREM {Key} (CountAttempted: {Count}) Removed: {Removed}", key, rv.Length, result);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis SREM failed for key {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.KeyExpireAsync(key, expiry);
            _logger.LogInformation("Redis EXPIRE {Key} (Expiry: {Expiry})", key, expiry);
            return result;
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis EXPIRE failed for key {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: prefix + "*");
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            _logger.LogInformation("Redis DELETE BY PREFIX {Prefix}*", prefix);
        }
        catch (Exception ex) when (IsRedisException(ex))
        {
            _logger.LogError(ex, "Redis DELETE BY PREFIX failed for prefix {Prefix}", prefix);
            throw;
        }
    }

    private static bool IsRedisException(Exception ex)
        => ex is RedisConnectionException
        || ex is RedisTimeoutException
        || ex is RedisServerException;
}