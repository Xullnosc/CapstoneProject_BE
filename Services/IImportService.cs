using BusinessObjects.DTOs;
using Services.Helpers;

namespace Services
{
    public interface IImportService
    {
        Task<ImportResult<WhitelistImportDTO>> ImportWhitelistFromExcel(
            Stream excelStream,
            string uploaderEmail,
            List<WhitelistRowOverrideDTO>? rowOverrides = null);

        Task SaveWhitelistBatchAsync(ImportResult<WhitelistImportDTO> importResult, string fileUrl, string uploaderEmail);
    }
}
