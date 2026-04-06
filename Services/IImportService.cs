using BusinessObjects.DTOs;
using Services.Helpers;

namespace Services
{
    public interface IImportService
    {
        Task<ImportResult<WhitelistImportDTO>> ImportWhitelistFromExcel(
            Stream excelStream,
            int semesterId,
            string uploaderEmail,
            List<WhitelistRowOverrideDTO>? rowOverrides = null);

        Task SaveWhitelistBatchAsync(ImportResult<WhitelistImportDTO> importResult, int semesterId, string fileUrl, string originalFileName, string uploaderEmail);

        Task<List<ImportBatchDTO>> GetImportBatchesBySemesterAsync(int semesterId);
    }
}
