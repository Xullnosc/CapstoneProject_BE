using BusinessObjects.DTOs;
using Repositories;

namespace Services
{
    public class ImportService : IImportService
    {
        private readonly IWhitelistRepository _whitelistRepository;

        public ImportService(IWhitelistRepository whitelistRepository)
        {
            _whitelistRepository = whitelistRepository;
        }

        public async Task<List<WhitelistImportDTO>> ImportWhitelistFromExcel(Stream excelStream)
        {
            var importHelper = new Helpers.ImportHelper();
            var importedWhiteLists = importHelper.ImportWhitelistFromExcel(excelStream);
            return await Task.FromResult(importedWhiteLists);
        }
    }
}
