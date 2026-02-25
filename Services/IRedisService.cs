using System.ComponentModel.DataAnnotations;

public interface IRedisService
{
    Task<bool> SetValueAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteValueAsync(string key, CancellationToken cancellationToken = default);
}