using AutoMapper;
using BusinessObjects.Models;
using Services.DTOs;
using BusinessObjects.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;

namespace Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User -> UserInfoDTO
            CreateMap<User, UserInfoDTO>()
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null)
                );

            // Whitelist -> WhitelistDTO
            CreateMap<Whitelist, WhitelistDTO>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Campus, opt => opt.MapFrom(src => CampusConstants.MapCodeToFullName(src.Campus)))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));

            // Team -> TeamSimpleDTO (Minimal info for lists)
            CreateMap<Team, TeamSimpleDTO>()
                .ForMember(
                    dest => dest.MemberCount,
                    opt => opt.MapFrom(src => src.Teammembers != null ? src.Teammembers.Count : 0)
                );

            // ArchivedTeam -> TeamSimpleDTO
            CreateMap<ArchivedTeam, TeamSimpleDTO>()
                .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.OriginalTeamId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? "Archived"))
                .AfterMap(
                    (src, dest) =>
                    {
                        if (!string.IsNullOrEmpty(src.JsonData))
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(src.JsonData);
                                if (doc.RootElement.TryGetProperty("Members", out var members))
                                {
                                    dest.MemberCount = members.GetArrayLength();
                                }
                            }
                            catch
                            { /* Fallback to 0 if JSON is malformed */
                            }
                        }
                    }
                );

            // ArchivedWhitelist -> WhitelistDTO
            CreateMap<ArchivedWhitelist, WhitelistDTO>()
                .ForMember(dest => dest.WhitelistId, opt => opt.MapFrom(src => src.OriginalWhitelistId))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Campus, opt => opt.MapFrom(src => CampusConstants.MapCodeToFullName(src.Campus)))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode));

            // Semester -> SemesterDTO
            CreateMap<Semester, SemesterDTO>()
                .ForMember(
                    dest => dest.TeamCount,
                    opt => opt.MapFrom(src => src.Teams != null ? src.Teams.Count : 0)
                )
                .ForMember(
                    dest => dest.Teams,
                    opt => opt.MapFrom(src => src.Teams ?? new List<Team>())
                )
                .ForMember(
                    dest => dest.Whitelists,
                    opt => opt.MapFrom(src => src.Whitelists ?? new List<Whitelist>())
                );

            // Reverse map for Create/Update
            CreateMap<SemesterDTO, Semester>();
            CreateMap<SemesterCreateDTO, Semester>();

            CreateMap<Thesis, ThesisDTO>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.OwnerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.ThesisReview == null ? new List<ReviewDTO>() : new List<ReviewDTO>
                {
                    new ReviewDTO {
                        ReviewerId = src.ThesisReview.Reviewer1Id,
                        ReviewerName = src.ThesisReview.Reviewer1 != null ? src.ThesisReview.Reviewer1.FullName : null,
                        Decision = src.ThesisReview.Reviewer1Decision ?? "Pending",
                        Comment = src.ThesisReview.Reviewer1Comment,
                        FileUrl = src.ThesisReview.Reviewer1FileUrl,
                        ReviewedAt = src.ThesisReview.Reviewer1Date ?? DateTime.MinValue,
                        ThesisId = src.ThesisId
                    },
                    new ReviewDTO {
                        ReviewerId = src.ThesisReview.Reviewer2Id,
                        ReviewerName = src.ThesisReview.Reviewer2 != null ? src.ThesisReview.Reviewer2.FullName : null,
                        Decision = src.ThesisReview.Reviewer2Decision ?? "Pending",
                        Comment = src.ThesisReview.Reviewer2Comment,
                        FileUrl = src.ThesisReview.Reviewer2FileUrl,
                        ReviewedAt = src.ThesisReview.Reviewer2Date ?? DateTime.MinValue,
                        ThesisId = src.ThesisId
                    }
                }.Where(r => r.ReviewerId != null && r.ReviewerId != 0).ToList()))
                .ForMember(dest => dest.Histories, opt => opt.MapFrom(src => src.ThesisHistories));

            // ThesisReview -> ReviewDTO
            CreateMap<ThesisReview, ReviewDTO>()
                .ForMember(dest => dest.ReviewerName, opt => opt.Ignore());

            // ThesisHistory → ThesisHistoryDTO
            CreateMap<ThesisHistory, ThesisHistoryDTO>()
                .ForMember(dest => dest.UploaderName, opt => opt.MapFrom(src => src.UploadedByUser != null ? src.UploadedByUser.FullName : null));

            // Checklist
            CreateMap<Checklist, ChecklistDTO>();
            CreateMap<ChecklistCreateDTO, Checklist>();

            // AccessLog
            CreateMap<AccessLog, AccessLogDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.User != null && src.User.Role != null ? src.User.Role.RoleName : null));
        }
    }
}
