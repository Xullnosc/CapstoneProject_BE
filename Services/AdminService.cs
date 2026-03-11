using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Services.DTOs;

namespace Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ISystemUserCredentialRepository _credentialRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly FctmsContext _context;

    public AdminService(
        IUserRepository userRepository,
        ISystemUserCredentialRepository credentialRepository,
        ISemesterRepository semesterRepository,
        FctmsContext context)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _semesterRepository = semesterRepository;
        _context = context;
    }

    public async Task<List<HodAccountDTO>> GetHodAccountsAsync(string? search)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role != null && u.Role.RoleName == CampusConstants.Roles.HOD);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.FullName != null && u.FullName.Contains(term))
            );
        }

        var users = await query
            .OrderBy(u => u.UserId)
            .ToListAsync();

        var userIds = users.Select(u => u.UserId).ToList();
        var creds = await _context.SystemUserCredentials
            .AsNoTracking()
            .Where(c => userIds.Contains(c.UserId))
            .ToListAsync();

        var credByUserId = creds.ToDictionary(c => c.UserId, c => c);

        return users.Select(u =>
        {
            credByUserId.TryGetValue(u.UserId, out var cred);
            return new HodAccountDTO
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Username = cred?.Username,
                HasCredential = cred != null,
                LastLogin = u.LastLogin,
                CreatedAt = u.CreatedAt,
                UpdatedAt = cred?.UpdatedAt
            };
        }).ToList();
    }

    public async Task CreateOrUpdateHodAsync(CreateOrUpdateHodDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("FullName, Email, Username and Password are required.");

        var roles = await _semesterRepository.GetAllRolesAsync();
        var hodRole = roles.FirstOrDefault(r => r.RoleName == CampusConstants.Roles.HOD);
        if (hodRole == null)
            throw new InvalidOperationException("HOD role not found in database.");

        var existingUser = await _userRepository.GetByEmailAsync(dto.Email.Trim());
        User user;

        if (existingUser != null)
        {
            if (existingUser.Role?.RoleName != CampusConstants.Roles.HOD)
                throw new InvalidOperationException($"User with email {dto.Email} exists but is not HOD. Cannot assign HOD credentials.");
            user = existingUser;
            user.FullName = dto.FullName?.Trim() ?? user.FullName;
            await _userRepository.UpdateAsync(user);
        }
        else
        {
            user = new User
            {
                Email = dto.Email.Trim(),
                FullName = dto.FullName?.Trim() ?? dto.Email,
                RoleId = hodRole.RoleId,
                IsAuthorized = true,
                CreatedAt = DateTime.UtcNow
            };
            user = await _userRepository.AddAsync(user);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
        var credByUser = await _credentialRepository.GetByUserIdAsync(user.UserId);
        var credByUsername = await _credentialRepository.GetByUsernameAsync(dto.Username.Trim());

        if (credByUsername != null && credByUsername.UserId != user.UserId)
            throw new InvalidOperationException($"Username '{dto.Username}' is already used by another user.");

        if (credByUser != null)
        {
            credByUser.Username = dto.Username.Trim();
            credByUser.PasswordHash = passwordHash;
            credByUser.UpdatedAt = DateTime.UtcNow;
            await _credentialRepository.UpdateAsync(credByUser);
        }
        else
        {
            await _credentialRepository.AddAsync(new SystemUserCredential
            {
                UserId = user.UserId,
                Username = dto.Username.Trim(),
                PasswordHash = passwordHash
            });
        }
    }
}
