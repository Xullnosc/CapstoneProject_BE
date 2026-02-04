using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Services.Helpers
{
    public interface IImportHelper
    {
        List<WhitelistImportDTO> ImportWhitelistFromExcel(Stream excelStream);
    }
}
