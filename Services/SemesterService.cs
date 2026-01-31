using BusinessObjects.DTOs;
using BusinessObjects.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Repositories;

namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IArchivingService _archivingService;
        private readonly IMapper _mapper;

        public SemesterService(ISemesterRepository semesterRepository, IArchivingService archivingService, IMapper mapper)
        {
            _semesterRepository = semesterRepository;
            _archivingService = archivingService;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves all semesters with aggregated team and student counts.
        /// Live semesters count from active data, ended semesters count from archived data.
        /// </summary>
        /// <returns>List of semester DTOs with counts filtered by Student role</returns>
        public async Task<List<SemesterDTO>> GetAllSemestersAsync()
        {
            var semesters = await _semesterRepository.GetAllSemestersAsync();
            var semesterDTOs = _mapper.Map<List<SemesterDTO>>(semesters);

            if (semesterDTOs == null || !semesterDTOs.Any())
                return new List<SemesterDTO>();

            var semesterIds = semesterDTOs.Select(s => s.SemesterId).ToList();

            var allArchivedTeams = await _archivingService.GetArchivedTeamsBySemesterIdsAsync(semesterIds);
            var allArchivedWhitelists = await _archivingService.GetArchivedWhitelistsBySemesterIdsAsync(semesterIds);
            
            var archivedTeamsBySemester = (allArchivedTeams ?? new List<ArchivedTeam>())
                .GroupBy(x => x.SemesterId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var archivedWhitelistsBySemester = (allArchivedWhitelists ?? new List<ArchivedWhitelist>())
                .GroupBy(x => x.SemesterId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Gets Student Role ID for filtering
            int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

            foreach (var dto in semesterDTOs)
            {
                // Add Active Count (assumed mapped) + Archived Count for Teams
                int activeTeamCount = dto.Teams?.Count ?? 0;
                int archivedTeamCount = 0;
                if (archivedTeamsBySemester.TryGetValue(dto.SemesterId, out var archivedTeamsList))
                {
                    archivedTeamCount = archivedTeamsList.Count;
                }

                dto.TeamCount = activeTeamCount + archivedTeamCount;

                // Student Count Logic:
                // Live/Upcoming -> Count Whitelist (Role = Student)
                // Ended -> Count ArchivedWhitelist (Role = Student)
                
                int liveStudentCount = dto.Whitelists?
                    .Count(w => w.RoleId == studentRoleId) ?? 0;

                int archivedStudentCount = 0;
                if (archivedWhitelistsBySemester.TryGetValue(dto.SemesterId, out var archivedWlList))
                {
                    archivedStudentCount = archivedWlList.Count(w => w.RoleId == studentRoleId);
                }

                dto.WhitelistCount = liveStudentCount + archivedStudentCount;
                
                // Set IsArchived flag if any archived data exists
                dto.IsArchived = archivedTeamCount > 0 || archivedStudentCount > 0;
                
                // CRITICAL OPTIMIZATION: Clear the Teams and Whitelists lists for the Dashboard view.
                dto.Teams = new List<TeamSimpleDTO>(); 
                dto.Whitelists = new List<WhitelistDTO>();
            }

            return semesterDTOs;
        }

        public async Task<SemesterDTO?> GetSemesterByIdAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(id);
            if (semester == null) return null;

            var dto = _mapper.Map<SemesterDTO>(semester);
            
            // Ensure lists are initialized even if AutoMapper set them to null (unlikely with new Profile config but safer)
            dto.Teams ??= new List<TeamSimpleDTO>();
            dto.Whitelists ??= new List<WhitelistDTO>();

            // Fetch archived data
            var archivedTeams = await _archivingService.GetArchivedTeamsBySemesterAsync(id);
            var archivedWhitelists = await _archivingService.GetArchivedWhitelistsBySemesterIdsAsync(new List<int> { id });

            int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

            // 1. Merge Teams
            int archivedTeamCount = 0;
            if (archivedTeams != null && archivedTeams.Any())
            {
                archivedTeamCount = archivedTeams.Count;
                var archivedTeamDTOs = _mapper.Map<List<TeamSimpleDTO>>(archivedTeams);
                dto.Teams.AddRange(archivedTeamDTOs);
            }

            // 2. Merge Whitelists
            int archivedStudentCount = 0;
            if (archivedWhitelists != null && archivedWhitelists.Any())
            {
                archivedStudentCount = archivedWhitelists.Count(w => w.RoleId == studentRoleId);
                var archivedWlDTOs = _mapper.Map<List<WhitelistDTO>>(archivedWhitelists);
                
                // Manually populate RoleName for archived entries (since they lack navigation property)
                var roles = await _semesterRepository.GetAllRolesAsync();
                var roleDict = roles.ToDictionary(r => r.RoleId, r => r.RoleName);
                
                foreach (var wlDto in archivedWlDTOs)
                {
                    if (wlDto.RoleId.HasValue && roleDict.TryGetValue(wlDto.RoleId.Value, out var roleName))
                    {
                        wlDto.RoleName = roleName;
                    }
                }
                
                dto.Whitelists.AddRange(archivedWlDTOs);
            }

            // 3. Calculate Counts (Total = Live + Archived)
            int liveTeamCount = semester.Teams?.Count ?? 0;
            int liveStudentCount = semester.Whitelists?.Count(w => w.RoleId == studentRoleId) ?? 0;

            dto.TeamCount = liveTeamCount + archivedTeamCount;
            dto.WhitelistCount = liveStudentCount + archivedStudentCount;
            
            // 4. Set IsArchived flag
            dto.IsArchived = archivedTeamCount > 0 || archivedStudentCount > 0;

            return dto;
        }

        public async Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            await ValidateSemesterLogicAsync(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            // Force IsActive to false on create. Must be started manually.
            semester.IsActive = false; 
            var createdSemester = await _semesterRepository.CreateSemesterAsync(semester);
            return _mapper.Map<SemesterDTO>(createdSemester);
        }

        public async Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            await ValidateSemesterLogicAsync(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null && existing.SemesterId != semesterCreateDTO.SemesterId)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            var semester = _mapper.Map<Semester>(semesterCreateDTO);

            await _semesterRepository.UpdateSemesterAsync(semester);
        }

        private async Task ValidateSemesterLogicAsync(SemesterCreateDTO dto)
        {
            var code = dto.SemesterCode.ToUpper();
            var name = dto.SemesterName.ToLower();

            // 1. Check Format
            if (code.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^(SP|SU|FA)\\d{2}$"))
            {
                throw new ArgumentException("Semester Code must be in format SPxx, SUxx, or FAxx (e.g., SP24).");
            }

            var prefix = code.Substring(0, 2);

            // 2. Check Season Match
            if (prefix == "SP" && !name.Contains("spring")) throw new ArgumentException("Code 'SP' (Spring) requires 'Spring' in Semester Name.");
            if (prefix == "SU" && !name.Contains("summer")) throw new ArgumentException("Code 'SU' (Summer) requires 'Summer' in Semester Name.");
            if (prefix == "FA" && !name.Contains("fall")) throw new ArgumentException("Code 'FA' (Fall) requires 'Fall' in Semester Name.");

            // 3. Check Date Overlap (Optimized to use Database AnyAsync)
            bool isOverlap = await _semesterRepository.IsOverlapAsync(dto.StartDate, dto.EndDate, dto.SemesterId > 0 ? dto.SemesterId : null);
            if (isOverlap)
            {
                throw new InvalidOperationException($"Semester dates overlap with another existing semester.");
            }
        }

        /// <summary>
        /// Activates a target semester and automatically ends all currently active semesters.
        /// Archives data from ending semesters before deactivation.
        /// </summary>
        /// <param name="id">ID of the semester to activate</param>
        /// <exception cref="KeyNotFoundException">Thrown when semester with given ID is not found</exception>
        public async Task StartSemesterAsync(int id)
        {
            var options = new System.Transactions.TransactionOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            using var transaction = new System.Transactions.TransactionScope(
                System.Transactions.TransactionScopeOption.Required,
                options,
                System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                var targetSemester = await _semesterRepository.GetSemesterByIdAsync(id);
                if (targetSemester == null) throw new KeyNotFoundException($"Semester {id} not found");

                // 1. Deactivate all other active semesters
                var allSemesters = await _semesterRepository.GetAllSemestersAsync();
                var currentActiveIds = allSemesters.Where(s => s.IsActive).Select(s => s.SemesterId).ToList();

                foreach (var activeId in currentActiveIds)
                {
                    if (activeId == id) continue; // If target is already active, skip

                    // Reuse EndSemesterAsync to Archive and Deactivate
                    // Nested TransactionScope works fine (Ambient transaction)
                    await EndSemesterAsync(activeId);
                }

                // 2. Activate target semester
                // CRITICAL FIX: Reload fresh entity to ensure tracking state is clean before Update
                var semesterToActivate = await _semesterRepository.GetSemesterByIdAsync(id);
                if (semesterToActivate != null && !semesterToActivate.IsActive)
                {
                    semesterToActivate.IsActive = true;
                    await _semesterRepository.UpdateSemesterAsync(semesterToActivate);
                }

                transaction.Complete();
            }
            catch (Exception)
            {
                throw;
            }
        }



        /// <summary>
        /// Ends a semester by deactivating it and archiving its associated data (teams and whitelists).
        /// </summary>
        /// <param name="id">ID of the semester to end</param>
        /// <exception cref="KeyNotFoundException">Thrown when semester with given ID is not found</exception>
        public async Task EndSemesterAsync(int id)
        {
            var options = new System.Transactions.TransactionOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            using var transaction = new System.Transactions.TransactionScope(
                System.Transactions.TransactionScopeOption.Required,
                options,
                System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                var semester = await _semesterRepository.GetSemesterByIdAsync(id);
                if (semester == null)
                {
                    throw new KeyNotFoundException($"Semester with ID {id} not found.");
                }

                // 1. Mark as Inactive
                semester.IsActive = false;
                await _semesterRepository.UpdateSemesterAsync(semester);

                // 2. Archive Data
                await _archivingService.ArchiveSemesterAsync(id);

                transaction.Complete();
            }
            catch (Exception)
            {
                // Transaction will auto-rollback if not completed
                throw;
            }
        }
    }
}
