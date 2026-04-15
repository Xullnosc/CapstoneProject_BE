using Services.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IUserService
    {
        Task<UserInfoDTO?> GetProfileAsync(int userId);
        Task<List<UserInfoDTO>> SearchStudentsAsync(string term, int currentUserId, int? teamId = null);
        Task<List<UserInfoDTO>> SearchLecturersAsync(string term, int currentUserId, int? teamId = null);
        Task<UserInfoDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO profileDto);
        Task<int> EnsureUserExistsAsync(int userId);
    }
}
