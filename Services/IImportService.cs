using BusinessObjects.DTOs;
using Services.Helpers;

namespace Services
{
    public interface IImportService
    {
        Task<ImportResult<WhitelistImportDTO>> ImportWhitelistFromExcel(Stream excelStream);
        Task SaveWhitelistBatchAsync(ImportResult<WhitelistImportDTO> importResult, string fileUrl, string? uploadedBy = null);
    }
}
