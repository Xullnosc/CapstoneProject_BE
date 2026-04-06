using BusinessObjects.DTOs;
using BusinessObjects.Models;
using BusinessObjects;
using AutoMapper;
using Repositories;
using Microsoft.Extensions.Configuration;
using BusinessObjects.Interfaces;


namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ICampusContextService _campusContextService;
        private readonly System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public SemesterService(
            ISemesterRepository semesterRepository, 
            IMapper mapper,
            IUserRepository userRepository,
            IRedisService redisService,
            IConfiguration configuration,
            ILecturerRepository lecturerRepository,
            IWhitelistRepository whitelistRepository,
            ICampusContextService campusContextService)
        {
            _semesterRepository = semesterRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _redisService = redisService;
            _configuration = configuration;
            _lecturerRepository = lecturerRepository;
            _whitelistRepository = whitelistRepository;
            _campusContextService = campusContextService;
        }

        /// <summary>
        /// Retrieves all semesters with aggregated team and student counts.
        /// Live semesters count from active data, ended semesters count from archived data.
        /// </summary>
        /// <returns>List of semester DTOs with counts filtered by Student role</returns>
        public async Task<List<SemesterDTO>> GetAllSemestersAsync()
        {
            var campusId = _campusContextService.GetCurrentCampusId()?.ToString() ?? "global";
            string cacheKey = $"fctms:semester_v2:all:{campusId}";
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

                // Gets Student Role ID for filtering
                int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

                foreach (var dto in semesterDTOs)
                {
                    // Add Count for Teams from Live data (including all statuses)
                    dto.TeamCount = dto.Teams?.Count ?? 0;
                    dto.ActiveTeamCount = dto.Teams?
                        .Count(t => string.Equals(t.Status, CampusConstants.TeamStatus.Active, StringComparison.OrdinalIgnoreCase)) ?? 0;

                    // Student Count Logic:
                    // Count Whitelist (Role = Student) directly from navigation property
                    dto.WhitelistCount = dto.Whitelists?
                        .Count(w => w.RoleId == studentRoleId) ?? 0;
                    
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

        /// <summary>
        /// Retrieves a page of semesters with aggregated team and student counts.
        /// Results are cached per-page and per-campus.
        /// </summary>
        public async Task<PagedResult<SemesterDTO>> GetAllSemestersPaginatedAsync(int page, int pageSize)
        {
            var campusId = _campusContextService.GetCurrentCampusId()?.ToString() ?? "global";
            string cacheKey = $"fctms:semester_v2:paged:{campusId}:{page}:{pageSize}";
            var cached = await _redisService.GetObjectAsync<PagedResult<SemesterDTO>>(cacheKey);
            if (cached != null) return cached;

            await _semaphore.WaitAsync();
            try
            {
                // Double-check cache after acquiring semaphore
                cached = await _redisService.GetObjectAsync<PagedResult<SemesterDTO>>(cacheKey);
                if (cached != null) return cached;

                // Get paged results from DAO
                var pagedSemesters = await _semesterRepository.GetAllSemestersAsync(page, pageSize);
                
                if (pagedSemesters.Items == null || !pagedSemesters.Items.Any())
                    return new PagedResult<SemesterDTO>(new List<SemesterDTO>(), pagedSemesters.TotalCount, page, pageSize);

                var semesterDTOs = _mapper.Map<List<SemesterDTO>>(pagedSemesters.Items);

                // Gets Student Role ID for filtering
                int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

                foreach (var dto in semesterDTOs)
                {
                    // Add Count for Teams from Live data (including all statuses)
                    dto.TeamCount = dto.Teams?.Count ?? 0;
                    dto.ActiveTeamCount = dto.Teams?
                        .Count(t => string.Equals(t.Status, CampusConstants.TeamStatus.Active, StringComparison.OrdinalIgnoreCase)) ?? 0;

                    // Student Count Logic:
                    // Count Whitelist (Role = Student) directly from navigation property
                    dto.WhitelistCount = dto.Whitelists?
                        .Count(w => w.RoleId == studentRoleId) ?? 0;
                    
                    // CRITICAL OPTIMIZATION: Clear the Teams and Whitelists lists for the Dashboard view.
                    dto.Teams = new List<TeamSimpleDTO>(); 
                    dto.Whitelists = new List<WhitelistDTO>();
                }

                var result = new PagedResult<SemesterDTO>(semesterDTOs, pagedSemesters.TotalCount, page, pageSize);

                var ttlStr = _configuration["RedisSettings:SemesterTTLMinutes"];
                int ttlMinutes;
                if (!int.TryParse(ttlStr, out ttlMinutes)) ttlMinutes = 30;
                ttlMinutes = System.Math.Max(1, ttlMinutes);
                await _redisService.SetObjectAsync(cacheKey, result, System.TimeSpan.FromMinutes(ttlMinutes), System.Threading.CancellationToken.None);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<SemesterDTO?> GetSemesterByIdAsync(int id)
        {
            var campusId = _campusContextService.GetCurrentCampusId()?.ToString() ?? "global";
            string cacheKey = $"fctms:semester_v2:id:{id}:{campusId}";
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

                int studentRoleId = await _semesterRepository.GetStudentRoleIdAsync();

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
                    // Populate Avatars and Reviewer status (Directly from database)
                    await PopulateWhitelistsAvatarsAndReviewersAsync(dto.Whitelists);
                }

                // 3. Calculate Counts from Live data only
                int liveTeamCount = semester.Teams?.Count ?? 0;
                int liveActiveTeams = semester.Teams?
                    .Count(t => string.Equals(t.Status, CampusConstants.TeamStatus.Active, StringComparison.OrdinalIgnoreCase)) ?? 0;
                int liveStudentCount = semester.Whitelists?.Count(w => w.RoleId == studentRoleId) ?? 0;

                dto.TeamCount = liveTeamCount;
                dto.ActiveTeamCount = liveActiveTeams; 
                dto.WhitelistCount = liveStudentCount;

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

        public async Task<SemesterDTO?> GetCurrentSemesterAsync()
        {
            var semester = await _semesterRepository.GetCurrentSemesterAsync();
            return _mapper.Map<SemesterDTO>(semester);
        }

        public async Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            await ValidateSemesterLogicAsync(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            // CRITICAL: Only allow creating new semester if the old one is CLOSED.
            if (await _semesterRepository.HasActiveSemesterAsync())
            {
                throw new InvalidOperationException("Hệ thống hiện đang có một kỳ học đang hoạt động (Open/In Progress). Bạn vui lòng Đóng (Close) kỳ học hiện tại trước khi tạo kỳ học mới.");
            }

            var campusId = _campusContextService.GetCurrentCampusId() 
                ?? throw new System.InvalidOperationException("Hành động này yêu cầu Campus Context hợp lệ. Super Admin phải chọn Campus cụ thể.");

            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            semester.CampusId = campusId;
            // Semester bắt đầu ở trạng thái 'Open' — tất cả hoạt động được phép.
            semester.Status = CampusConstants.SemesterStatus.Open;
            var createdSemester = await _semesterRepository.CreateSemesterAsync(semester);

            await InvalidateSemesterCacheAsync();
            return _mapper.Map<SemesterDTO>(createdSemester);
        }

        public async Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            var currentSemester = await _semesterRepository.GetSemesterByIdSimpleAsync(semesterCreateDTO.SemesterId);
            if (currentSemester == null)
            {
                throw new KeyNotFoundException($"Semester with ID {semesterCreateDTO.SemesterId} not found.");
            }

            // Chỉ cho phép sửa metadata khi kỳ học đang mở (Open)
            if (!CampusConstants.SemesterStatus.IsOpenStage(currentSemester.Status?.Trim()))
            {
                throw new InvalidOperationException($"Chỉ có thể chỉnh sừa thông tin kỳ học khi đang ở trạng thái 'Open'. Trạng thái hiện tại: {currentSemester.Status}");
            }

            await ValidateSemesterLogicAsync(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null && existing.SemesterId != semesterCreateDTO.SemesterId)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            // Preserve fields not carried by SemesterCreateDTO.
            semester.CampusId = currentSemester.CampusId;
            semester.Status = currentSemester.Status;

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
        /// [DEPRECATED] Semester không còn dùng trạng thái 'Upcoming'.
        /// Semester tạo ra là 'Open' ngay. Method này giữ lại để backward compat nếu có record cũ.
        /// </summary>
        [Obsolete("Semester no longer uses Upcoming status. Kept for backward compatibility only.")]
        public async Task StartSemesterAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(id);
            if (semester == null) throw new KeyNotFoundException($"Semester {id} not found");

            // Hỗ trợ cả Upcoming lẫn các record cũ khác (Active/Open)
            if (!CampusConstants.SemesterStatus.IsOpenStage(semester.Status?.Trim()) &&
                !string.Equals(semester.Status?.Trim(), CampusConstants.SemesterStatus.Upcoming, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Không thể bắt đầu kỳ học. Trạng thái hiện tại: {semester.Status}");
            }

            // CRITICAL: Only allow starting if no other active semester exists
            if (await _semesterRepository.HasActiveSemesterAsync())
            {
                throw new InvalidOperationException("Hệ thống hiện đang có một kỳ học khác đang hoạt động. Vui lòng đóng kỳ học đó trước.");
            }

            semester.Status = CampusConstants.SemesterStatus.Open;
            await _semesterRepository.UpdateSemesterAsync(semester);
            await InvalidateSemesterCacheAsync(id);
        }

        /// <summary>
        /// Chuyển kỳ học từ Open → In Progress (Chốt đề tài — khóa tất cả đăng ký mới).
        /// </summary>
        public async Task LockSubmissionAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(id);
            if (semester == null) throw new KeyNotFoundException($"Semester {id} not found");

            if (!CampusConstants.SemesterStatus.IsOpenStage(semester.Status?.Trim()))
            {
                throw new InvalidOperationException($"Chỉ có thể chốt đề tài khi kỳ học đang mở (Open). Trạng thái hiện tại: {semester.Status}");
            }

            semester.Status = CampusConstants.SemesterStatus.InProgress;
            await _semesterRepository.UpdateSemesterAsync(semester);
            await InvalidateSemesterCacheAsync(id);
        }

        /// <summary>
        /// [DEPRECATED] Không còn dùng — Lifecycle đã đơn giản hóa xuống 3 stage.
        /// Chỉ giữ lại để backward compat với frontend chưa cập nhật.
        /// </summary>
        [Obsolete("Use LockSubmissionAsync instead. This method is deprecated in the 3-stage lifecycle.")]
        public async Task LockAllUpdatesAsync(int id)
        {
            // No-op in new 3-stage lifecycle. Redirect to CloseSemesterAsync if needed.
            throw new InvalidOperationException("LockAllUpdates is deprecated. Use LockSubmission (Open→InProgress) then CloseSemester (InProgress→Closed).");
        }

        /// <summary>
        /// Kết thúc kỳ học — chuyển sang Closed (chỉ xem).
        /// Hỗ trợ cả InProgress lẫn các giá trị cũ (Review Thesis, Review Middle Semester) để backward compat.
        /// </summary>
        public async Task CloseSemesterAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdSimpleAsync(id);
            if (semester == null) throw new KeyNotFoundException($"Semester {id} not found");

            // Chỉ cho phép đóng từ trạng thái Locked (InProgress / legacy Review states)
            if (!CampusConstants.SemesterStatus.IsLockedStage(semester.Status?.Trim()))
            {
                throw new InvalidOperationException($"Phải chốt đề tài (In Progress) trước khi đóng kỳ học. Trạng thái hiện tại: {semester.Status}");
            }

            semester.Status = CampusConstants.SemesterStatus.Closed;
            await _semesterRepository.UpdateSemesterAsync(semester);
            await InvalidateSemesterCacheAsync(id);
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
            
            // 2. Fetch Avatars and Reviewer status for all collected whitelists
            await PopulateWhitelistsAvatarsAndReviewersAsync(allWhitelists);

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

        public async Task<PagedResult<WhitelistDTO>> GetOrphanedStudentsAsync(int semesterId, int page, int pageSize, string? search = null)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(semesterId);
            if (semester == null) throw new KeyNotFoundException($"Semester {semesterId} not found");

            var pagedOrphaned = await _semesterRepository.GetOrphanedStudentsAsync(semesterId, page, pageSize, search);
            var dtos = _mapper.Map<List<WhitelistDTO>>(pagedOrphaned.Items);

            // Populate avatars (same pattern as GetWhitelistsPaginatedAsync)
            var emails = dtos
                .Where(w => !string.IsNullOrWhiteSpace(w.Email))
                .Select(w => w.Email.Trim())
                .Distinct()
                .ToList();

            if (emails.Any())
            {
                var users = await _userRepository.GetUsersByEmailsAsync(emails);
                var avatarDict = users
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Avatar, StringComparer.OrdinalIgnoreCase);

                foreach (var wl in dtos)
                {
                    if (!string.IsNullOrWhiteSpace(wl.Email) && avatarDict.TryGetValue(wl.Email.Trim(), out var avatar))
                    {
                        wl.Avatar = avatar;
                    }
                }
            }

            foreach (var wl in dtos)
            {
                wl.Campus = CampusConstants.MapCodeToFullName(wl.Campus);
            }

            return new PagedResult<WhitelistDTO>(dtos, pagedOrphaned.TotalCount, page, pageSize);
        }

        public async Task InvalidateSemesterCacheAsync(int? id = null)
        {
            // Clear all semester-related caches (all campuses)
            // This is safer and ensures no stale data remains after a structural change.
            await _redisService.RemoveByPrefixAsync("fctms:semester_v2:");
        }

        private async Task PopulateWhitelistsAvatarsAndReviewersAsync(List<WhitelistDTO> whitelists)
        {
            if (whitelists == null || !whitelists.Any()) return;

            var emails = whitelists
                .Where(w => !string.IsNullOrWhiteSpace(w.Email))
                .Select(w => w.Email.Trim().ToLower())
                .Distinct()
                .ToList();

            if (!emails.Any()) return;

            // Fetch users for avatars
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var avatarDict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    string key = user.Email.Trim().ToLower();
                    if (!avatarDict.ContainsKey(key))
                    {
                        avatarDict[key] = user.Avatar;
                    }
                }
            }

            // Fetch lecturers for reviewer status
            var lecturers = await _lecturerRepository.GetByEmailsAsync(emails);
            var reviewerDict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var lecturer in lecturers)
            {
                if (!string.IsNullOrEmpty(lecturer.Email))
                {
                    string key = lecturer.Email.Trim().ToLower();
                    if (!reviewerDict.ContainsKey(key))
                    {
                        reviewerDict[key] = lecturer.IsReviewer;
                    }
                }
            }

            foreach (var wl in whitelists)
            {
                if (string.IsNullOrWhiteSpace(wl.Email)) continue;

                string emailKey = wl.Email.Trim().ToLower();
                
                // Only overwrite avatar if current is empty/"N/A" and new one is better
                bool hasNoAvatar = string.IsNullOrWhiteSpace(wl.Avatar) || wl.Avatar == "N/A";
                if (hasNoAvatar && avatarDict.TryGetValue(emailKey, out var avatar) && !string.IsNullOrWhiteSpace(avatar))
                {
                    wl.Avatar = avatar;
                }

                // Update Reviewer status
                if (reviewerDict.TryGetValue(emailKey, out var isReviewer))
                {
                    wl.IsReviewer = isReviewer;
                }

                // Map Campus Code to Name
                if (string.IsNullOrWhiteSpace(wl.Campus) && wl.CampusId > 0)
                {
                    wl.Campus = CampusConstants.MapIdToFullName(wl.CampusId);
                }
                else
                {
                    wl.Campus = CampusConstants.MapCodeToFullName(wl.Campus);
                }
            }
        }
    }
}
