using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Services.DTOs;

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
                )
                .ForMember(
                    dest => dest.PhoneNumber,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.PhoneNumber : null
                        )
                )
                .ForMember(
                    dest => dest.GithubLink,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.GithubLink : null
                        )
                )
                .ForMember(
                    dest => dest.LinkedInLink,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.LinkedInLink : null
                        )
                )
                .ForMember(
                    dest => dest.FacebookLink,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.FacebookLink : null
                        )
                )
                .ForMember(
                    dest => dest.DateOfBirth,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.DateOfBirth : null
                        )
                )
                .ForMember(
                    dest => dest.Gender,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.Gender : null
                        )
                )
                .ForMember(
                    dest => dest.Address,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.Address : null
                        )
                )
                .ForMember(
                    dest => dest.Major,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.Major : null
                        )
                )
                .ForMember(
                    dest => dest.PersonalId,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.PersonalId : null
                        )
                )
                .ForMember(
                    dest => dest.PlaceOfBirth,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.PlaceOfBirth : null
                        )
                )
                .ForMember(
                    dest => dest.EnrollmentYear,
                    opt =>
                        opt.MapFrom(src =>
                            src.AccountDetail != null ? src.AccountDetail.EnrollmentYear : null
                        )
                );

            // Whitelist -> WhitelistDTO
            CreateMap<Whitelist, WhitelistDTO>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null)
                )
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(
                    dest => dest.Campus,
                    opt => opt.MapFrom(src => CampusConstants.MapCodeToFullName(src.Campus))
                )
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));

            // Team -> TeamSimpleDTO (Minimal info for lists)
            CreateMap<Team, TeamSimpleDTO>()
                .ForMember(
                    dest => dest.MemberCount,
                    opt => opt.MapFrom(src => src.Teammembers != null ? src.Teammembers.Count : 0)
                );

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
                .ForMember(
                    dest => dest.OwnerName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null)
                )
                .ForMember(
                    dest => dest.OwnerEmail,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Email : null)
                )
                .ForMember(
                    dest => dest.OwnerAvatar,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Avatar : null)
                )
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(
                    dest => dest.MentorEmail1,
                    opt => opt.MapFrom(src => src.Mentor1 != null ? src.Mentor1.Email : null)
                )
                .ForMember(
                    dest => dest.MentorEmail2,
                    opt => opt.MapFrom(src => src.Mentor2 != null ? src.Mentor2.Email : null)
                )
                .ForMember(dest => dest.Histories, opt => opt.MapFrom(src => src.ThesisHistories));

            // ThesisHistory → ThesisHistoryDTO
            CreateMap<ThesisHistory, ThesisHistoryDTO>()
                .ForMember(
                    dest => dest.UploaderName,
                    opt =>
                        opt.MapFrom(src =>
                            src.UploadedByUser != null ? src.UploadedByUser.FullName : null
                        )
                )
                .ForMember(
                    dest => dest.UploaderAvatar,
                    opt =>
                        opt.MapFrom(src =>
                            src.UploadedByUser != null ? src.UploadedByUser.Avatar : null
                        )
                );

            // Checklist
            CreateMap<Checklist, ChecklistDTO>();
            CreateMap<ChecklistCreateDTO, Checklist>();

            // AccessLog
            CreateMap<AccessLog, AccessLogDTO>()
                .ForMember(
                    dest => dest.FullName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null)
                )
                .ForMember(
                    dest => dest.RoleName,
                    opt =>
                        opt.MapFrom(src =>
                            src.User != null && src.User.Role != null
                                ? src.User.Role.RoleName
                                : null
                        )
                );

            // Notification
            CreateMap<Notification, NotificationDTO>()
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead == true));

            // System Logs
            CreateMap<SystemErrorLog, SystemErrorLogDTO>().ReverseMap();
        }
    }
}
