using Services.DTOs;

namespace Services;

public interface IAdminService
{
    Task<List<HodAccountDTO>> GetHodAccountsAsync(string? search);
    Task CreateOrUpdateHodAsync(CreateOrUpdateHodDTO dto);
    Task DeleteHodAsync(int userId);
    Task UpdateHodEmailAsync(int userId, string newEmail);
}
