using BusinessObjects.Models;
using Repositories;
using Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class ThesisApplicationService : IThesisApplicationService
    {
        private readonly IThesisApplicationRepository _appRepo;
        private readonly IThesisRepository _thesisRepo;
        private readonly ITeamRepository _teamRepo;
        private readonly ISemesterRepository _semesterRepo;
        private readonly ITeamInvitationRepository _teamInvRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILecturerRepository _lecturerRepo;

        public ThesisApplicationService(
            IThesisApplicationRepository appRepo,
            IThesisRepository thesisRepo,
            ITeamRepository teamRepo,
            ISemesterRepository semesterRepo,
            ITeamInvitationRepository teamInvRepo,
            IUserRepository userRepo,
            ILecturerRepository lecturerRepo)
        {
            _appRepo = appRepo;
            _thesisRepo = thesisRepo;
            _teamRepo = teamRepo;
            _semesterRepo = semesterRepo;
            _teamInvRepo = teamInvRepo;
            _userRepo = userRepo;
            _lecturerRepo = lecturerRepo;
        }

        public async Task<ThesisApplicationDTO> SubmitApplicationAsync(int userId, string thesisId)
        {
            // 1. Find the active semester
            var semester = await _semesterRepo.GetCurrentSemesterAsync();
            if (semester == null)
                throw new InvalidOperationException("No active semester found.");

            // 2. Find team where user is Leader in the active semester
            var team = await _teamRepo.GetTeamByStudentIdAsync(userId, semester.SemesterId);
            if (team == null)
                throw new InvalidOperationException("You are not a member of any team in the current semester.");
            if (team.LeaderId != userId)
                throw new InvalidOperationException("Only the team leader can submit an application.");

            // 3. Find the thesis
            var thesis = await _thesisRepo.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            // 3.5. Validate: team must have exactly 5 members OR be a special team
            int memberCount = team.Teammembers?.Count ?? 0;
            if (memberCount < 5 && !team.IsSpecial)
            {
                throw new InvalidOperationException("Nhóm của bạn phải có đủ 5 thành viên hoặc là nhóm đặc biệt (Special Team) mới có thể đăng ký đề tài.");
            }

            // 4. Validate: thesis must be Published
            if (thesis.Status != "Published")
                throw new InvalidOperationException("Only theses with 'Published' status can be applied for.");

            // 5. Validate: team must not already have an Approved application in this semester
            var hasApproved = await _appRepo.HasApprovedInSemesterAsync(team.TeamId, semester.SemesterId);
            if (hasApproved)
                throw new InvalidOperationException("Your team already has an approved application in this semester.");

            // 6. Check if an application already exists (to handle re-applications after Cancelled/Rejected)
            var existing = await _appRepo.GetByThesisAndTeamAsync(thesisId, team.TeamId);
            if (existing != null)
            {
                if (existing.Status == "Pending" || existing.Status == "Approved")
                    throw new InvalidOperationException("Your team already has an active application for this thesis.");

                // If it was Cancelled or Rejected, we reuse the record to avoid unique constraint issues
                existing.Status = "Pending";
                existing.CreatedAt = DateTime.UtcNow;
                await _appRepo.UpdateAsync(existing);

                return new ThesisApplicationDTO
                {
                    Id = existing.Id,
                    ThesisId = existing.ThesisId,
                    ThesisTitle = thesis.Title,
                    ThesisOwnerName = thesis.User?.FullName,
                    ThesisOwnerAvatar = thesis.User?.Avatar,
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    Status = existing.Status,
                    CreatedAt = existing.CreatedAt
                };
            }

            // 7. Create if not exists
            var application = new ThesisApplication
            {
                ThesisId = thesisId,
                TeamId = team.TeamId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var created = await _appRepo.CreateAsync(application);

            return new ThesisApplicationDTO
            {
                Id = created.Id,
                ThesisId = created.ThesisId,
                ThesisTitle = thesis.Title,
                ThesisOwnerName = thesis.User?.FullName,
                ThesisOwnerAvatar = thesis.User?.Avatar,
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Status = created.Status,
                CreatedAt = created.CreatedAt
            };
        }

        public async Task CancelApplicationAsync(int userId, int applicationId)
        {
            var app = await _appRepo.GetByIdAsync(applicationId);
            if (app == null)
                throw new KeyNotFoundException("Application not found.");

            // Validate: user must be Leader of the team
            if (app.Team.LeaderId != userId)
                throw new UnauthorizedAccessException("Only the team leader can cancel this application.");

            // Validate: only Pending can be cancelled
            if (app.Status != "Pending")
                throw new InvalidOperationException("Only applications with 'Pending' status can be cancelled.");

            // Soft-delete: set status to Cancelled
            app.Status = "Cancelled";
            await _appRepo.UpdateAsync(app);
        }

        public async Task<List<ThesisApplicationDTO>> GetApplicationsByTeamAsync(int userId, int? teamId = null)
        {
            int resolvedTeamId;

            if (teamId.HasValue)
            {
                resolvedTeamId = teamId.Value;
            }
            else
            {
                var semester = await _semesterRepo.GetCurrentSemesterAsync();
                if (semester == null)
                    return new List<ThesisApplicationDTO>();

                var team = await _teamRepo.GetTeamByStudentIdAsync(userId, semester.SemesterId);
                if (team == null)
                    return new List<ThesisApplicationDTO>();

                resolvedTeamId = team.TeamId;
            }

            var apps = await _appRepo.GetByTeamIdAsync(resolvedTeamId);

            return apps.Select(a => new ThesisApplicationDTO
            {
                Id = a.Id,
                ThesisId = a.ThesisId,
                ThesisTitle = a.Thesis?.Title,
                ThesisOwnerName = a.Thesis?.User?.FullName,
                ThesisOwnerAvatar = a.Thesis?.User?.Avatar,
                TeamId = a.TeamId,
                TeamName = a.Team?.TeamName,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        // ===== F093: Approve / Reject / Get by Thesis =====

        public async Task<object> GetApplicationsByThesisAsync(
            int userId, string thesisId, string? status, string? search, int page, int limit)
        {
            // Authorization: thesis owner OR reviewer (mentor) of the owner's team can view
            var thesis = await _thesisRepo.GetThesisByIdAsync(thesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            bool isAuthorized = false;
            if (thesis.UserId == userId)
            {
                isAuthorized = true;
            }
            else
            {
                var semester = await _semesterRepo.GetCurrentSemesterAsync();
                if (semester != null && thesis.UserId.HasValue)
                {
                    var ownerTeam = await _teamRepo.GetTeamByStudentIdAsync(thesis.UserId.Value, semester.SemesterId);
                    if (ownerTeam != null && (ownerTeam.MentorId == userId || ownerTeam.MentorId2 == userId))
                    {
                        isAuthorized = true;
                    }
                }
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You are not authorized to view applications for this thesis.");

            var (items, totalCount) = await _appRepo.GetByThesisIdPagedAsync(thesisId, status, search, page, limit);

            var dtos = items.Select(a => new
            {
                a.Id,
                a.ThesisId,
                a.TeamId,
                TeamName = a.Team?.TeamName,
                TeamCode = a.Team?.TeamCode,
                LeaderName = a.Team?.Leader?.FullName,
                a.Status,
                a.CreatedAt,
                Members = a.Team?.Teammembers?.Select(m => new
                {
                    m.StudentId,
                    FullName = m.Student?.FullName,
                    StudentCode = m.Student?.StudentCode
                }).ToList()
            }).ToList();

            return new
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                Limit = limit,
                TotalPages = (int)Math.Ceiling((double)totalCount / limit)
            };
        }

        public async Task ApproveApplicationAsync(int userId, int applicationId)
        {
            var app = await _appRepo.GetByIdAsync(applicationId);
            if (app == null)
                throw new KeyNotFoundException("Application not found.");

            if (app.Thesis == null)
                throw new KeyNotFoundException("Thesis associated with this application not found.");

            // Authorization: only thesis owner or their team mentor can approve
            bool isAuthorized = false;
            if (app.Thesis.UserId == userId)
            {
                isAuthorized = true;
            }
            else
            {
                var authSemester = await _semesterRepo.GetCurrentSemesterAsync();
                if (authSemester != null && app.Thesis.UserId.HasValue)
                {
                    var ownerTeam = await _teamRepo.GetTeamByStudentIdAsync(app.Thesis.UserId.Value, authSemester.SemesterId);
                    if (ownerTeam != null && (ownerTeam.MentorId == userId || ownerTeam.MentorId2 == userId))
                    {
                        isAuthorized = true;
                    }
                }
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("Only the thesis owner or their team mentor can approve applications.");

            if (app.Status != "Pending")
                throw new InvalidOperationException("Only Pending applications can be approved.");

            // 0. Resolve LecturerId for the approver (MentorId1 references Lecturers table)
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("Approver user not found.");
            
            var lecturer = await _lecturerRepo.GetByEmailAsync(user.Email);
            if (lecturer == null) throw new KeyNotFoundException("Approver lecturer record not found. Please ensure your account is linked to a Lecturer record.");

            // Validate: GV is mentor of fewer than 4 teams in current semester
            var semester = await _semesterRepo.GetCurrentSemesterAsync();
            if (semester == null)
                throw new InvalidOperationException("No active semester found.");

            var mentorTeamCount = await _teamInvRepo.GetMentorActiveTeamCountAsync(userId, semester.SemesterId);
            if (mentorTeamCount >= 4)
                throw new InvalidOperationException("You have reached the maximum number of mentored teams (4) for this semester.");

            // 1. Approve this application
            app.Status = "Approved";
            
            // Critical Fix: Update the thesis linked to the application with the correct LecturerId
            // app.Thesis is already loaded via Include in GetByIdAsync
            if (app.Thesis != null)
            {
                app.Thesis.TeamId = app.TeamId;
                app.Thesis.MentorId1 = lecturer.LecturerId;
                app.Thesis.Status = "Registered";
            }

            await _appRepo.UpdateAsync(app);

            // 2. Reject all other Pending applications for the same thesis
            await _appRepo.RejectAllPendingByThesisIdExceptAsync(app.ThesisId, app.Id);

            // 2.5. Cancel all other Pending applications for the SAME TEAM (other theses)
            await _appRepo.CancelAllPendingByTeamIdExceptAsync(app.TeamId, app.Id);

            // 5. Set GV as team's MentorId
            var team = await _teamRepo.GetByIdAsync(app.TeamId);
            if (team != null)
            {
                team.MentorId = userId;
                await _teamRepo.UpdateAsync(team);
            }
        }

        public async Task RejectApplicationAsync(int userId, int applicationId)
        {
            var app = await _appRepo.GetByIdAsync(applicationId);
            if (app == null)
                throw new KeyNotFoundException("Application not found.");

            // Authorization: only thesis owner or their team mentor can reject
            var thesis = await _thesisRepo.GetThesisByIdAsync(app.ThesisId);
            if (thesis == null)
                throw new KeyNotFoundException("Thesis not found.");

            bool isAuthorized = false;
            if (thesis.UserId == userId)
            {
                isAuthorized = true;
            }
            else
            {
                var semester = await _semesterRepo.GetCurrentSemesterAsync();
                if (semester != null && thesis.UserId.HasValue)
                {
                    var ownerTeam = await _teamRepo.GetTeamByStudentIdAsync(thesis.UserId.Value, semester.SemesterId);
                    if (ownerTeam != null && (ownerTeam.MentorId == userId || ownerTeam.MentorId2 == userId))
                    {
                        isAuthorized = true;
                    }
                }
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("Only the thesis owner or their team mentor can reject applications.");

            if (app.Status != "Pending")
                throw new InvalidOperationException("Only Pending applications can be rejected.");

            app.Status = "Rejected";
            await _appRepo.UpdateAsync(app);
        }
    }
}
