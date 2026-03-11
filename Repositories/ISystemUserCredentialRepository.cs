using BusinessObjects.Models;

namespace Repositories;

public interface ISystemUserCredentialRepository
{
    Task<SystemUserCredential?> GetByUsernameAsync(string username);
    Task<SystemUserCredential?> GetByUserIdAsync(int userId);
    Task<SystemUserCredential> AddAsync(SystemUserCredential credential);
    Task UpdateAsync(SystemUserCredential credential);
    Task DeleteAsync(SystemUserCredential credential);
}
