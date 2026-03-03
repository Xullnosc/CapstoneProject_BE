using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Services.Helpers
{
    public interface IImportHelper
    {
        ImportResult<WhitelistImportDTO> ImportWhitelistFromExcel(Stream excelStream);
    }
}
