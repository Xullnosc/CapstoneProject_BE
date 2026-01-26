using AutoMapper;
using BusinessObjects.Models;
using Services.DTOs;
using BusinessObjects.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User -> UserInfoDTO
            CreateMap<User, UserInfoDTO>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null));

            // Whitelist -> WhitelistDTO
            CreateMap<Whitelist, WhitelistDTO>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null));

            // Team -> TeamSimpleDTO (Minimal info for lists)
            CreateMap<Team, TeamSimpleDTO>()
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Teammembers.Count));

            // Semester -> SemesterDTO
            CreateMap<Semester, SemesterDTO>()
                .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Teams))
                .ForMember(dest => dest.Whitelists, opt => opt.MapFrom(src => src.Whitelists));

            // Reverse map for Create/Update
            CreateMap<SemesterDTO, Semester>();
            CreateMap<SemesterCreateDTO, Semester>();
        }
    }
}
