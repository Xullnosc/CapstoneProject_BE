using BusinessObjects.Models;

namespace DataAccess;

public interface ISystemUserCredentialDAO
{
    Task<SystemUserCredential?> GetByUsernameAsync(string username);
    Task<SystemUserCredential?> GetByIdentifierAsync(string identifier);
    Task<SystemUserCredential?> GetByUserIdAsync(int userId);
    Task<List<SystemUserCredential>> GetByUserIdsAsync(List<int> userIds);
    Task<SystemUserCredential> AddAsync(SystemUserCredential credential);
    Task UpdateAsync(SystemUserCredential credential);
    Task DeleteAsync(SystemUserCredential credential);
}
