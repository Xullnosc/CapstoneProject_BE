using System.ComponentModel.DataAnnotations;

public interface IRedisService
{
    Task<bool> SetValueAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> SetObjectAsync<T>(string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
    Task<T?> GetObjectAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    Task<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<long> SetAddAsync(string key, IEnumerable<string> values, CancellationToken cancellationToken = default);
    Task<string[]?> SetMembersAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<long> SetRemoveAsync(string key, IEnumerable<string> values, CancellationToken cancellationToken = default);
    Task<bool> ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}