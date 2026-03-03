using Services.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IUserService
    {
        Task<List<UserInfoDTO>> SearchStudentsAsync(string term, int currentUserId, int? teamId = null);
        Task<List<UserInfoDTO>> SearchLecturersAsync(string term, int currentUserId, int? teamId = null);
    }
}
