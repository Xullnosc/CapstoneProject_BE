using BusinessObjects.Models;
using DataAccess;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class ThesisApplicationRepository : IThesisApplicationRepository
    {
        private readonly IThesisApplicationDAO _dao;

        public ThesisApplicationRepository(IThesisApplicationDAO dao)
        {
            _dao = dao;
        }

        public async Task<ThesisApplication> CreateAsync(ThesisApplication app)
            => await _dao.CreateAsync(app);

        public async Task<ThesisApplication?> GetByIdAsync(int id)
            => await _dao.GetByIdAsync(id);

        public async Task<List<ThesisApplication>> GetByTeamIdAsync(int teamId)
            => await _dao.GetByTeamIdAsync(teamId);

        public async Task<ThesisApplication?> GetActiveByThesisAndTeamAsync(string thesisId, int teamId)
            => await _dao.GetActiveByThesisAndTeamAsync(thesisId, teamId);

        public async Task<bool> HasApprovedInSemesterAsync(int teamId, int semesterId)
            => await _dao.HasApprovedInSemesterAsync(teamId, semesterId);

        public async Task UpdateAsync(ThesisApplication app)
            => await _dao.UpdateAsync(app);

        public async Task<(List<ThesisApplication> Items, int TotalCount)> GetByThesisIdPagedAsync(
            string thesisId, string? status, string? search, int page, int limit)
            => await _dao.GetByThesisIdPagedAsync(thesisId, status, search, page, limit);

        public async Task RejectAllPendingByThesisIdExceptAsync(string thesisId, int exceptId)
            => await _dao.RejectAllPendingByThesisIdExceptAsync(thesisId, exceptId);
    }
}
