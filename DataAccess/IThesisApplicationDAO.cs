using BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IThesisApplicationDAO
    {
        Task<ThesisApplication> CreateAsync(ThesisApplication app);
        Task<ThesisApplication?> GetByIdAsync(int id);
        Task<List<ThesisApplication>> GetByTeamIdAsync(int teamId);
        Task<BusinessObjects.Models.ThesisApplication?> GetActiveByThesisAndTeamAsync(string thesisId, int teamId);
        Task<BusinessObjects.Models.ThesisApplication?> GetByThesisAndTeamAsync(string thesisId, int teamId);
        Task<bool> HasApprovedInSemesterAsync(int teamId, int semesterId);
        Task UpdateAsync(ThesisApplication app);
        Task<(List<ThesisApplication> Items, int TotalCount)> GetByThesisIdPagedAsync(string thesisId, string? status, string? search, int page, int limit);
        Task RejectAllPendingByThesisIdExceptAsync(string thesisId, int exceptId);
    }
}
