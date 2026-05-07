using Services.DTOs;
using Repositories;
using BusinessObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;
using AutoMapper;
using System;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ITeamInvitationRepository _teamInvitationRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IAccountDetailRepository _accountDetailRepository;
        private readonly IMapper _mapper;

        public UserService(
            IUserRepository userRepository, 
            ISemesterRepository semesterRepository, 
            ITeamMemberRepository teamMemberRepository,
            ITeamInvitationRepository teamInvitationRepository,
            IWhitelistRepository whitelistRepository,
            ITeamRepository teamRepository,
            ILecturerRepository lecturerRepository,
            IAccountDetailRepository accountDetailRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _semesterRepository = semesterRepository;
            _teamMemberRepository = teamMemberRepository;
            _teamInvitationRepository = teamInvitationRepository;
            _whitelistRepository = whitelistRepository;
            _teamRepository = teamRepository;
            _lecturerRepository = lecturerRepository;
            _accountDetailRepository = accountDetailRepository;
            _mapper = mapper;
        }

        public async Task<UserInfoDTO?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            var roles = await _whitelistRepository.GetByEmailAsync(user.Email);
            var result = _mapper.Map<UserInfoDTO>(user);
            if (roles != null)
                result.RoleName = roles.Role?.RoleName ?? result.RoleName;

            return result;
        }

        public async Task<List<UserInfoDTO>> SearchStudentsAsync(string term, int currentUserId, int? teamId = null)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<UserInfoDTO>();
            }

            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            var semesterId = currentSemester?.SemesterId ?? 0;

            if (semesterId == 0) return new List<UserInfoDTO>();

            var whitelists = await _whitelistRepository.SearchAsync(term, semesterId);
            var emails = whitelists.Select(w => w.Email).ToList();
            var existingUsers = await _userRepository.GetUsersByEmailsAsync(emails);

            var result = new List<UserInfoDTO>();

            foreach (var w in whitelists)
            {
                var existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(w.Email, System.StringComparison.OrdinalIgnoreCase));

                // Exclude current user
                if (existingUser != null && existingUser.UserId == currentUserId) continue;

                // Ensure user has Student role
                if (w.Role?.RoleName != CampusConstants.Roles.Student && w.RoleId != 3) continue;

                // Only include Qualified students
                if (!string.Equals(w.Status, CampusConstants.WhitelistStatus.Qualified, System.StringComparison.OrdinalIgnoreCase)) continue;

                var dto = new UserInfoDTO
                {
                    UserId = existingUser?.UserId ?? -w.WhitelistId, // Use negative id for unique UI key if not logged in
                    Email = w.Email,
                    FullName = w.FullName ?? existingUser?.FullName,
                    StudentCode = w.StudentCode ?? existingUser?.StudentCode,
                    Avatar = w.Avatar ?? existingUser?.Avatar
                };

                if (existingUser != null && semesterId > 0)
                {
                    dto.HasTeam = await _teamMemberRepository.IsStudentInTeamAsync(existingUser.UserId, semesterId);
                }

                if (teamId.HasValue && !dto.HasTeam && existingUser != null)
                {
                    var existingInvitation = await _teamInvitationRepository.GetByTeamAndReceiverAsync(teamId.Value, existingUser.UserId);
                    if (existingInvitation != null && existingInvitation.Status == CampusConstants.InvitationStatus.Pending)
                    {
                        dto.PendingInvitationId = existingInvitation.InvitationId;
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<List<UserInfoDTO>> SearchLecturersAsync(string term, int currentUserId, int? teamId = null)
        {
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            if (currentSemester == null) return new List<UserInfoDTO>();

            // 1. Search from Global Lecturer Pool (Primary Source)
            var globalLecturers = await _lecturerRepository.SearchAsync(term ?? string.Empty);
            
            // 2. Search from Whitelist (Secondary Source / Fallback)
            var whitelistedLecturers = await _whitelistRepository.GetBySemesterIdAsync(currentSemester.SemesterId);
            
            // Internal helper class for merging
            List<LecturerSearchItem> combinedList = new List<LecturerSearchItem>();

            foreach (var l in globalLecturers)
            {
                combinedList.Add(new LecturerSearchItem 
                { 
                    TempId = -l.LecturerId, // Unique negative ID
                    Email = l.Email, 
                    FullName = l.FullName, 
                    RoleId = 2, 
                    Avatar = l.Avatar 
                });
            }

            if (whitelistedLecturers != null)
            {
                foreach (var w in whitelistedLecturers)
                {
                    if (w.RoleId == 2 || (w.Role != null && w.Role.RoleName == CampusConstants.Roles.Lecturer))
                    {
                        // Check search term for Whitelist entries
                        bool matches = string.IsNullOrWhiteSpace(term) || 
                                       (w.Email != null && w.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) || 
                                       (w.FullName != null && w.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
                                       
                        if (matches && !combinedList.Any(c => c.Email.Equals(w.Email, StringComparison.OrdinalIgnoreCase)))
                        {
                            combinedList.Add(new LecturerSearchItem 
                            { 
                                TempId = -(w.WhitelistId + 1000000), // Ensure different range from Lecturer Pool
                                Email = w.Email, 
                                FullName = w.FullName, 
                                RoleId = w.RoleId, 
                                Avatar = w.Avatar 
                            });
                        }
                    }
                }
            }

            var result = new List<UserInfoDTO>();

            Team? team = null;
            if (teamId.HasValue)
            {
                team = await _teamRepository.GetByIdAsync(teamId.Value);
            }

            // Batch fetch all User records
            var emails = combinedList.Select(c => c.Email).Distinct().ToList();
            var users = await _userRepository.GetUsersByEmailsAsync(emails);
            var userMap = users.ToDictionary(u => u.Email, u => u, StringComparer.OrdinalIgnoreCase);

            foreach (var item in combinedList)
            {
                userMap.TryGetValue(item.Email, out User? user);
                
                if (user != null && user.UserId == currentUserId) continue;

                if (team != null && user != null)
                {
                    if (team.MentorId == user.UserId || team.MentorId2 == user.UserId) continue;
                }

                var dto = new UserInfoDTO
                {
                    UserId = user?.UserId ?? item.TempId, // Use User record ID or unique Temp ID
                    Email = item.Email,
                    FullName = item.FullName ?? string.Empty,
                    Avatar = user?.Avatar ?? item.Avatar,
                    HasTeam = false
                };

                if (teamId.HasValue && user != null)
                {
                    var existingInvitation = await _teamInvitationRepository.GetByTeamAndMentorAsync(teamId.Value, user.UserId);
                    if (existingInvitation != null && existingInvitation.Status == CampusConstants.InvitationStatus.Pending)
                    {
                        dto.PendingInvitationId = existingInvitation.InvitationId;
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<UserInfoDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO profileDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            if (!string.IsNullOrWhiteSpace(profileDto.FullName))
                user.FullName = profileDto.FullName;

            await _userRepository.UpdateAsync(user);

            // Account detail (chỉ dùng bảng account_detail)
            var detail = await _accountDetailRepository.GetByUserIdAsync(userId);
            if (detail == null)
            {
                detail = new AccountDetail
                {
                    UserId = userId,
                    PhoneNumber = profileDto.PhoneNumber,
                    GithubLink = profileDto.GithubLink,
                    LinkedInLink = profileDto.LinkedInLink,
                    FacebookLink = profileDto.FacebookLink,
                    DateOfBirth = profileDto.DateOfBirth,
                    Gender = profileDto.Gender,
                    Address = profileDto.Address,
                    Major = profileDto.Major,
                    PersonalId = profileDto.PersonalId,
                    PlaceOfBirth = profileDto.PlaceOfBirth,
                    EnrollmentYear = profileDto.EnrollmentYear
                };
                await _accountDetailRepository.AddAsync(detail);
            }
            else
            {
                detail.PhoneNumber = profileDto.PhoneNumber;
                detail.GithubLink = profileDto.GithubLink;
                detail.LinkedInLink = profileDto.LinkedInLink;
                detail.FacebookLink = profileDto.FacebookLink;
                if (profileDto.DateOfBirth.HasValue) detail.DateOfBirth = profileDto.DateOfBirth;
                detail.Gender = profileDto.Gender;
                detail.Address = profileDto.Address;
                detail.Major = profileDto.Major;
                detail.PersonalId = profileDto.PersonalId;
                detail.PlaceOfBirth = profileDto.PlaceOfBirth;
                if (profileDto.EnrollmentYear.HasValue) detail.EnrollmentYear = profileDto.EnrollmentYear;
                await _accountDetailRepository.UpdateAsync(detail);
            }

            user.AccountDetail = detail;
            var roles = await _whitelistRepository.GetByEmailAsync(user.Email);
            var result = _mapper.Map<UserInfoDTO>(user);
            if (roles != null)
            {
                result.RoleName = roles.Role?.RoleName ?? result.RoleName;
            }

            return result;
        }

        private class LecturerSearchItem
        {
            public int TempId { get; set; } // Added TempId
            public string Email { get; set; } = null!;
            public string? FullName { get; set; }
            public int? RoleId { get; set; }
            public string? Avatar { get; set; }
        }
    }
}

