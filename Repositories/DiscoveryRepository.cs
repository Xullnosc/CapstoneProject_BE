using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Helpers;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class DiscoveryRepository : IDiscoveryRepository
    {
        private readonly IDiscoveryDAO _discoveryDAO;

        public DiscoveryRepository(IDiscoveryDAO discoveryDAO)
        {
            _discoveryDAO = discoveryDAO;
        }

        public async Task<(List<DiscoveryStudentDto> Items, int TotalCount)> GetLookingStudentsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            var (students, totalCount) = await _discoveryDAO.GetLookingStudentsAsync(semesterId, campusId, currentUserId, skillFilter, searchQuery, page, pageSize);
            
            var dtos = students.Select(u => new DiscoveryStudentDto(
                u.UserId,
                u.FullName,
                u.StudentCode,
                string.IsNullOrWhiteSpace(u.Avatar) 
                    ? $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(u.FullName)}&background=random&color=fff" 
                    : u.Avatar,
                u.AccountDetail?.Major,
                u.UserSkills.Select(s => new UserSkillDto(s.SkillId, s.SkillTag, s.SkillLevel)).ToList()
            )).ToList();

            return (dtos, totalCount);
        }

        public async Task<(List<DiscoveryTeamDto> Items, int TotalCount)> GetOpenTeamsAsync(
            int semesterId, int campusId, int currentUserId, string? skillFilter, string? searchQuery, int page, int pageSize)
        {
            var isUserInTeam = await _discoveryDAO.IsUserInTeamAsync(currentUserId, semesterId);
            var (teams, totalCount) = await _discoveryDAO.GetOpenTeamsAsync(semesterId, campusId, currentUserId, skillFilter, searchQuery, page, pageSize);

            var dtos = teams.Select(t => new DiscoveryTeamDto(
                t.TeamId,
                t.TeamName,
                string.IsNullOrWhiteSpace(t.TeamAvatar)
                    ? $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(t.TeamName)}&background=random&color=fff"
                    : t.TeamAvatar,
                t.LeaderId,
                t.Description,
                t.Teammembers.Count,
                5, 
                t.Teammembers
                    .SelectMany(tm => tm.Student.UserSkills)
                    .Select(s => s.SkillTag)
                    .Distinct()
                    .ToList(),
                DisplayHelper.FormatTeamCode(t.TeamCode),
                t.Teaminvitations.Any(i => i.InvitedBy == currentUserId && i.Type == CampusConstants.InvitationType.JoinRequest && i.Status == CampusConstants.InvitationStatus.Pending),
                isUserInTeam
            )).ToList();

            return (dtos, totalCount);
        }

        public async Task<List<string>> GetTopSkillsAsync(int count)
        {
            return await _discoveryDAO.GetTopSkillsAsync(count);
        }



        public async Task<bool> IsUserInTeamAsync(int userId, int semesterId)
        {
            return await _discoveryDAO.IsUserInTeamAsync(userId, semesterId);
        }

        public async Task UpdateUserSkillsAsync(int userId, List<UserSkill> skills)
        {
            await _discoveryDAO.UpdateUserSkillsAsync(userId, skills);
        }

        public async Task<List<UserSkill>> GetUserSkillsAsync(int userId)
        {
            return await _discoveryDAO.GetUserSkillsAsync(userId);
        }
    }
}
