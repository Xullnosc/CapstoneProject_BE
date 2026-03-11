using BusinessObjects.DTOs;
using BusinessObjects.Models;
using BusinessObjects;
using AutoMapper;
using Repositories;
using Microsoft.Extensions.Configuration;


namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IArchivingService _archivingService;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public SemesterService(
            ISemesterRepository semesterRepository, 
            IArchivingService archivingService, 
            IMapper mapper,
            IUserRepository userRepository,
            IRedisService redisService,
            IConfiguration configuration,
            ILecturerRepository lecturerRepository,
            IWhitelistRepository whitelistRepository)
        {
            _semesterRepository = semesterRepository;
            _archivingService = archivingService;
            _mapper = mapper;
            _userRepository = userRepository;
            _redisService = redisService;
            _configuration = configuration;
            _lecturerRepository = lecturerRepository;
            _whitelistRepository = whitelistRepository;
        }

        /// <summary>
        /// Retrieves all semesters with aggregated team and student counts.
        /// Live semesters count from active data, ended semesters count from archived data.
        /// </summary>
        /// <returns>List of semester DTOs with counts filtered by Student role</returns>
        public async Task<List<SemesterDTO>> GetAllSemestersAsync()
        {
            const string cacheKey = "fctms:semester:all";
            var cached = await _redisService.GetObjectAsync<List<SemesterDTO>>(cacheKey);
            if (cached != null) return cached;

            await _semaphore.WaitAsync();
            try
            {
                // Double-check cache after acquiring semaphore
                cached = await _redisService.GetObjectAsync<List<SemesterDTO>>(cacheKey);
                if (cached != null) return cached;

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
                    int liveTeamTotal = dto.Teams?.Count ?? 0;
                    int liveActiveTeams = dto.Teams?
                        .Count(t => string.Equals(t.Status, CampusConstants.TeamStatus.Active, StringComparison.OrdinalIgnoreCase)) ?? 0;
                    int archivedTeamCount = 0;
                    if (archivedTeamsBySemester.TryGetValue(dto.SemesterId, out var archivedTeamsList))
                    {
                        archivedTeamCount = archivedTeamsList.Count;
                    }

                    dto.TeamCount = liveTeamTotal + archivedTeamCount;
                    dto.ActiveTeamCount = liveActiveTeams; // Only teams with Status = Active

                    // Student Count Logic:
                    // Status != Ended -> Count Whitelist (Role = Student)
                    // Status == Ended -> Count ArchivedWhitelist (Role = Student)
                    
                    int liveStudentCount = dto.Whitelists?
                        .Count(w => w.RoleId == studentRoleId) ?? 0;

                    int archivedStudentCount = 0;
                    if (archivedWhitelistsBySemester.TryGetValue(dto.SemesterId, out var archivedWlList))
                    {
                        archivedStudentCount = archivedWlList.Count(w => w.RoleId == studentRoleId);
                    }

                    dto.WhitelistCount = liveStudentCount + archivedStudentCount;
                    
                    // CRITICAL OPTIMIZATION: Clear the Teams and Whitelists lists for the Dashboard view.
                    dto.Teams = new List<TeamSimpleDTO>(); 
                    dto.Whitelists = new List<WhitelistDTO>();
                }

                var ttlStr = _configuration["RedisSettings:SemesterTTLMinutes"];
                int ttlMinutes;
                if (!int.TryParse(ttlStr, out ttlMinutes)) ttlMinutes = 30;
                ttlMinutes = System.Math.Max(1, ttlMinutes);
                await _redisService.SetObjectAsync(cacheKey, semesterDTOs, System.TimeSpan.FromMinutes(ttlMinutes), System.Threading.CancellationToken.None);

                return semesterDTOs;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<SemesterDTO?> GetSemesterByIdAsync(int id)
        {
            string cacheKey = $"fctms:semester:id:{id}";
            var cached = await _redisService.GetObjectAsync<SemesterDTO>(cacheKey);
            if (cached != null) return cached;

            await _semaphore.WaitAsync();
            try
            {
                // Double-check cache
                cached = await _redisService.GetObjectAsync<SemesterDTO>(cacheKey);
                if (cached != null) return cached;

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

                // 2. Add Global Lecturers
                var allDbRoles = await _semesterRepository.GetAllRolesAsync();
                var lecturerRole = allDbRoles.FirstOrDefault(r => string.Equals(r.RoleName, CampusConstants.Roles.Lecturer, StringComparison.OrdinalIgnoreCase));
                if (lecturerRole != null)
                {
                    // Using WhitelistRepository to get all whitelists with Lecturer role
                    var globalWhitelists = await _whitelistRepository.GetByRoleAsync(lecturerRole.RoleId);
                    var globalLecturers = globalWhitelists.Where(w => w.SemesterId == null).ToList();

                    // Convert to DTO
                    var globalLecturerDTOs = _mapper.Map<List<WhitelistDTO>>(globalLecturers ?? new List<Whitelist>());
                    foreach (var gl in globalLecturerDTOs)
                    {
                        // Prevent duplicates if there is a leftover legacy entry with a semester ID
                        if (!dto.Whitelists.Any(w => string.Equals(w.Email, gl.Email, StringComparison.OrdinalIgnoreCase)))
                        {
                            gl.RoleName = lecturerRole.RoleName;
                            dto.Whitelists.Add(gl);
                        }
                        else
                        {
                            // If a semester-specific entry already exists, consider transferring IsReviewer state visually
                            // (Though they want to move to purely global list, so this is just a safety check)
                        }
                    }
                }

                // POPULATE AVATARS FOR ALL WHITELISTS
                if (dto.Whitelists.Any())
                {
                    // Normalize emails (Trim)
                    var emails = dto.Whitelists
                        .Where(w => !string.IsNullOrWhiteSpace(w.Email))
                        .Select(w => w.Email.Trim())
                        .Distinct()
                        .ToList();

                    var users = await _userRepository.GetUsersByEmailsAsync(emails);
                    
                    // Use case-insensitive dictionary to match emails safely
                    var avatarDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.Email) && !avatarDict.ContainsKey(user.Email.Trim()))
                        {
                            avatarDict[user.Email.Trim()] = user.Avatar;
                        }
                    }

                    foreach (var wl in dto.Whitelists)
                    {
                        if (string.IsNullOrWhiteSpace(wl.Email)) continue;
                        string emailKey = wl.Email.Trim().ToLower();
                        bool hasNoAvatar = string.IsNullOrWhiteSpace(wl.Avatar) || wl.Avatar == "N/A";

                        if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var avatar) && !string.IsNullOrWhiteSpace(avatar))
                        {
                            wl.Avatar = avatar;
                        }

                        // Also ensure campus is mapped to full name for display
                        wl.Campus = CampusConstants.MapCodeToFullName(wl.Campus);
                    }
                }

                // 3. Calculate Counts (Total = Live + Archived)
                int liveTeamCount = semester.Teams?.Count ?? 0;
                int liveActiveTeams = semester.Teams?
                    .Count(t => string.Equals(t.Status, CampusConstants.TeamStatus.Active, StringComparison.OrdinalIgnoreCase)) ?? 0;
                int liveStudentCount = semester.Whitelists?.Count(w => w.RoleId == studentRoleId) ?? 0;

                dto.TeamCount = liveTeamCount + archivedTeamCount;
                dto.ActiveTeamCount = liveActiveTeams; // Only count Qualified teams
                dto.WhitelistCount = liveStudentCount + archivedStudentCount;

                var ttlStr = _configuration["RedisSettings:SemesterTTLMinutes"];
                int ttlMinutes2;
                if (!int.TryParse(ttlStr, out ttlMinutes2)) ttlMinutes2 = 30;
                ttlMinutes2 = System.Math.Max(1, ttlMinutes2);
                await _redisService.SetObjectAsync(cacheKey, dto, System.TimeSpan.FromMinutes(ttlMinutes2), System.Threading.CancellationToken.None);

                return dto;
            }
            finally
            {
                _semaphore.Release();
            }
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
            // Force Status to Upcoming. Must be started manually.
            semester.Status = "Upcoming";
            var createdSemester = await _semesterRepository.CreateSemesterAsync(semester);

            await InvalidateSemesterCacheAsync();
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
            await InvalidateSemesterCacheAsync(semester.SemesterId);
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

            // 3. Check Date Overlap (Optimized to use Database FirstOrDefaultAsync)
            var overlapSemester = await _semesterRepository.IsOverlapAsync(dto.StartDate, dto.EndDate, dto.SemesterId > 0 ? dto.SemesterId : null);
            if (overlapSemester != null)
            {
                throw new InvalidOperationException($"Semester dates overlap with another existing semester: '{overlapSemester.SemesterName}' ({overlapSemester.SemesterCode}).");
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
                var currentActiveIds = allSemesters.Where(s => s.Status == "Active").Select(s => s.SemesterId).ToList();

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
                if (semesterToActivate != null && semesterToActivate.Status != "Active")
                {
                    semesterToActivate.Status = "Active";
                    // Detach navigation properties to prevent EF tracking conflicts
                    semesterToActivate.Teams = null!;
                    semesterToActivate.Whitelists = null!;
                    await _semesterRepository.UpdateSemesterAsync(semesterToActivate);
                }

                transaction.Complete();
                await InvalidateSemesterCacheAsync(id);
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

                // 1. Mark as Ended (Always succeed)
                semester.Status = "Ended";
                // Detach navigation properties to prevent EF tracking conflicts
                semester.Teams = null!;
                semester.Whitelists = null!;
                await _semesterRepository.UpdateSemesterAsync(semester);

                // 2. Archive Data (Best effort - not blocking the status change)
                try 
                {
                    await _archivingService.ArchiveSemesterAsync(id);
                }
                catch (Exception)
                {
                    // Log warning but don't fail the transaction
                    // In a real app we'd log this: _logger.LogWarning("Archiving failed for semester {id}", id);
                }

                transaction.Complete();
                await InvalidateSemesterCacheAsync(id);
            }
            catch (Exception)
            {
                // Transaction will auto-rollback if not completed
                throw;
            }
        }

        public async Task<PagedResult<WhitelistDTO>> GetWhitelistsPaginatedAsync(int semesterId, int page, int pageSize, string? role = null, string? search = null)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(semesterId);
            if (semester == null) throw new KeyNotFoundException($"Semester {semesterId} not found");

            // 1. Collect all potential whitelists
            var allWhitelists = new List<WhitelistDTO>();

            // Live whitelists
            if (semester.Whitelists != null && semester.Whitelists.Any())
            {
                allWhitelists.AddRange(_mapper.Map<List<WhitelistDTO>>(semester.Whitelists));
            }

            // Archived whitelists
            var archivedWhitelists = await _archivingService.GetArchivedWhitelistsBySemesterIdsAsync(new List<int> { semesterId });
            if (archivedWhitelists != null && archivedWhitelists.Any())
            {
                var archivedDTOs = _mapper.Map<List<WhitelistDTO>>(archivedWhitelists);
                
                // Map RoleName for archived entries
                var roles = await _semesterRepository.GetAllRolesAsync();
                var roleDict = roles.ToDictionary(r => r.RoleId, r => r.RoleName);
                foreach (var wlDto in archivedDTOs)
                {
                    if (wlDto.RoleId.HasValue && roleDict.TryGetValue(wlDto.RoleId.Value, out var roleName))
                    {
                        wlDto.RoleName = roleName;
                    }
                }
                allWhitelists.AddRange(archivedDTOs);
            }

            // Global Lecturers (SemesterId is null)
            var allDbRoles = await _semesterRepository.GetAllRolesAsync();
            var lecturerRole = allDbRoles.FirstOrDefault(r => string.Equals(r.RoleName, CampusConstants.Roles.Lecturer, StringComparison.OrdinalIgnoreCase));
            if (lecturerRole != null)
            {
                var globalWhitelists = await _whitelistRepository.GetByRoleAsync(lecturerRole.RoleId);
                var globalLecturers = globalWhitelists.Where(w => w.SemesterId == null).ToList();
                var globalLecturerDTOs = _mapper.Map<List<WhitelistDTO>>(globalLecturers);
                foreach (var gl in globalLecturerDTOs)
                {
                    if (!allWhitelists.Any(w => string.Equals(w.Email, gl.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        gl.RoleName = lecturerRole.RoleName;
                        allWhitelists.Add(gl);
                    }
                }
            }

            // 2. Fetch Avatars for all collected whitelists (Directly from database)
            var emails = allWhitelists
                .Where(w => !string.IsNullOrWhiteSpace(w.Email))
                .Select(w => w.Email.Trim())
                .Distinct()
                .ToList();

            if (emails.Any())
            {
                var users = await _userRepository.GetUsersByEmailsAsync(emails);
                // Case-insensitive dictionary to match avatars safely
                var avatarDict = users
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Avatar, StringComparer.OrdinalIgnoreCase);

                foreach (var wl in allWhitelists)
                {
                    if (!string.IsNullOrWhiteSpace(wl.Email) && avatarDict.TryGetValue(wl.Email.Trim(), out var avatar))
                    {
                        wl.Avatar = avatar;
                    }
                }
            }

            // Campus mapping is now handled in MappingProfile, but for manual merges we ensure consistency
            foreach (var wl in allWhitelists)
            {
                wl.Campus = CampusConstants.MapCodeToFullName(wl.Campus);
            }

            // 3. Filter
            var filtered = allWhitelists.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                filtered = filtered.Where(w => string.Equals(w.RoleName, role, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                filtered = filtered.Where(w => 
                    (w.Email != null && w.Email.ToLower().Contains(s)) || 
                    (w.FullName != null && w.FullName.ToLower().Contains(s)) ||
                    (w.StudentCode != null && w.StudentCode.ToLower().Contains(s))
                );
            }

            // 4. Paginate
            var list = filtered.OrderBy(w => w.FullName ?? w.Email).ToList();
            int total = list.Count;
            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<WhitelistDTO>(items, total, page, pageSize);
        }

        public async Task InvalidateSemesterCacheAsync(int? id = null)
        {
            await _redisService.DeleteValueAsync("fctms:semester:all");
            if (id.HasValue)
            {
                await _redisService.DeleteValueAsync($"fctms:semester:id:{id.Value}");
            }
            else
            {
                // If no specific ID, we might need to invalidate all semester detail caches
                // but usually after Create, only "all" needs invalidation.
                // However, following the requirement for prefix based invalidation:
                await _redisService.RemoveByPrefixAsync("fctms:semester:");
            }
        }
    }
}
