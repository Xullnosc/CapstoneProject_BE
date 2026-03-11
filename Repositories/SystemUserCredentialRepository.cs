using BusinessObjects.Models;
using DataAccess;

namespace Repositories;

public class SystemUserCredentialRepository : ISystemUserCredentialRepository
{
    private readonly ISystemUserCredentialDAO _dao;

    public SystemUserCredentialRepository(ISystemUserCredentialDAO dao)
    {
        _dao = dao;
    }

    public Task<SystemUserCredential?> GetByUsernameAsync(string username) =>
        _dao.GetByUsernameAsync(username);

    public Task<SystemUserCredential?> GetByUserIdAsync(int userId) =>
        _dao.GetByUserIdAsync(userId);

    public Task<SystemUserCredential> AddAsync(SystemUserCredential credential) =>
        _dao.AddAsync(credential);

    public Task UpdateAsync(SystemUserCredential credential) =>
        _dao.UpdateAsync(credential);

    public Task DeleteAsync(SystemUserCredential credential) =>
        _dao.DeleteAsync(credential);
}
