using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Repositories
{
    public interface IImportRepository
    {
        Task<string?> GetUserCampusByEmailAsync(string normalizedEmail);
        Task<List<User>> GetUsersForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes);
        Task<List<Whitelist>> GetWhitelistsForConflictCheckAsync(List<string> normalizedEmails, List<string> normalizedStudentCodes);
        Task ReconcileSemesterAsync(int semesterId, List<WhitelistImportDTO> importedItems, int studentRoleId, DateTime now);
        Task AddImportBatchAsync(ImportBatch batch);
        Task<List<ImportBatch>> GetImportBatchesBySemesterAsync(int semesterId);
    }
}
