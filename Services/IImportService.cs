using BusinessObjects.DTOs;

namespace Services
{
    public interface IImportService
    {
        Task<List<WhitelistImportDTO>> ImportWhitelistFromExcel(Stream excelStream);
    }
}
