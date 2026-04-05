using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ImportRepository : IImportRepository
    {
        private readonly IImportDAO _importDAO;

        public ImportRepository(IImportDAO importDAO)
        {
            _importDAO = importDAO;
        }

        public Task<string?> GetUserCampusByEmailAsync(string normalizedEmail)
        {
            return _importDAO.GetUserCampusByEmailAsync(normalizedEmail);
        }

        public Task<List<User>> GetUsersForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes)
        {
            return _importDAO.GetUsersForConflictCheckAsync(normalizedEmails, normalizedStudentCodes);
        }

        public Task<List<Whitelist>> GetWhitelistsForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes)
        {
            return _importDAO.GetWhitelistsForConflictCheckAsync(normalizedEmails, normalizedStudentCodes);
        }

        public Task ReconcileSemesterAsync(int semesterId, List<WhitelistImportDTO> importedItems, int studentRoleId, DateTime now)
        {
            return _importDAO.ReconcileSemesterAsync(semesterId, importedItems, studentRoleId, now);
        }

        public Task AddImportBatchAsync(ImportBatch batch)
        {
            return _importDAO.AddImportBatchAsync(batch);
        }

        public Task<List<ImportBatch>> GetImportBatchesBySemesterAsync(int semesterId)
        {
            return _importDAO.GetImportBatchesBySemesterAsync(semesterId);
        }
    }
}
