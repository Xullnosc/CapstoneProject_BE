using Services.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IThesisApplicationService
    {
        Task<ThesisApplicationDTO> SubmitApplicationAsync(int userId, string thesisId);
        Task CancelApplicationAsync(int userId, int applicationId);
        Task<List<ThesisApplicationDTO>> GetApplicationsByTeamAsync(int userId, int? teamId = null);
        Task<object> GetApplicationsByThesisAsync(int userId, string thesisId, string? status, string? search, int page, int limit);
        Task ApproveApplicationAsync(int userId, int applicationId);
        Task RejectApplicationAsync(int userId, int applicationId);
    }
}
