using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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
                );

            // Whitelist -> WhitelistDTO
            CreateMap<Whitelist, WhitelistDTO>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null)
                );

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
                .ForMember(
                    dest => dest.WhitelistId,
                    opt => opt.MapFrom(src => src.OriginalWhitelistId)
                )
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId));

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
        }
    }
}
