using BusinessObjects.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IThesisFormService
    {
        Task<ThesisFormDTO> UploadThesisFormAsync(UploadThesisFormDTO req, string email);
        Task<ThesisFormDTO?> GetLatestFormAsync();
        Task<IEnumerable<ThesisFormHistoryDTO>> GetFormHistoriesAsync();
    }
}
