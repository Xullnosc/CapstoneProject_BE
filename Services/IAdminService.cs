using Services.DTOs;

namespace Services;

public interface IAdminService
{
    Task<List<HodAccountDTO>> GetHodAccountsAsync(string? search);
    Task CreateOrUpdateHodAsync(CreateOrUpdateHodDTO dto);
}
