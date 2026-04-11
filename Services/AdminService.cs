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
            .Include(u => u.CampusNavigation)
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
                UpdatedAt = cred?.UpdatedAt,
                Campus = u.CampusNavigation?.CampusName
            };
        }).ToList();
    }

    public async Task CreateOrUpdateHodAsync(CreateOrUpdateHodDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Username))
            throw new ArgumentException("Email and Username are required.");

        if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.Password))
             throw new ArgumentException("Password is required for new accounts.");

        var roles = await _semesterRepository.GetAllRolesAsync();
        var hodRole = roles.FirstOrDefault(r => r.RoleName == CampusConstants.Roles.HOD);
        if (hodRole == null)
            throw new InvalidOperationException("HOD role not found in database.");

        // Campus check
        var campusRef = await _context.Campuses.FirstOrDefaultAsync(c => c.CampusId == dto.CampusId);
        if (campusRef == null)
            throw new ArgumentException($"Campus with ID {dto.CampusId} not found.");

        // Check for existing HOD in this campus (1 HOD per campus rule)
        var existingHodInCampus = await _context.Users
            .AnyAsync(u => u.CampusId == dto.CampusId 
                && u.Role != null 
                && u.Role.RoleName == CampusConstants.Roles.HOD 
                && u.UserId != dto.UserId);
        
        if (existingHodInCampus)
            throw new InvalidOperationException($"Campus '{campusRef.CampusName}' already has an HOD. Please remove the existing HOD before assigning a new one.");

        User? user = null;
        if (dto.UserId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(dto.UserId.Value);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");
            
            if (user.Role?.RoleName != CampusConstants.Roles.HOD)
                throw new InvalidOperationException("User is not an HOD.");

            // Check if email changed and if new email is taken
            if (user.Email != dto.Email.Trim())
            {
                var existingWithEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim());
                if (existingWithEmail != null && existingWithEmail.UserId != user.UserId)
                    throw new InvalidOperationException($"Email '{dto.Email}' is already used by another user.");
            }

            user.Email = dto.Email.Trim();
            user.FullName = dto.FullName?.Trim() ?? user.FullName;
            user.CampusId = dto.CampusId;
            await _userRepository.UpdateAsync(user);
        }
        else
        {
            var existingWithEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim());
            if (existingWithEmail != null)
            {
                if (existingWithEmail.Role?.RoleName != CampusConstants.Roles.HOD)
                    throw new InvalidOperationException($"User with email {dto.Email} exists but is not HOD.");
                
                user = existingWithEmail;
                user.FullName = dto.FullName?.Trim() ?? user.FullName;
                user.CampusId = dto.CampusId;
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
                    CreatedAt = DateTime.UtcNow,
                    CampusId = dto.CampusId
                };
                user = await _userRepository.AddAsync(user);
            }
        }

        var credByUser = await _credentialRepository.GetByUserIdAsync(user.UserId);
        var credByUsername = await _credentialRepository.GetByUsernameAsync(dto.Username.Trim());

        if (credByUsername != null && credByUsername.UserId != user.UserId)
            throw new InvalidOperationException($"Username '{dto.Username}' is already used by another user.");

        if (credByUser != null)
        {
            credByUser.Username = dto.Username.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                credByUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
            }
            credByUser.UpdatedAt = DateTime.UtcNow;
            await _credentialRepository.UpdateAsync(credByUser);
        }
        else
        {
            await _credentialRepository.AddAsync(new SystemUserCredential
            {
                UserId = user.UserId,
                Username = dto.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(12)),
                CreatedAt = DateTime.UtcNow
            });
        }

        // --- HOD to Lecturer Synchronization ---
        var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == user.Email);
        if (lecturer == null)
        {
            lecturer = new Lecturer
            {
                Email = user.Email,
                FullName = user.FullName,
                Avatar = user.Avatar,
                CampusId = user.CampusId ?? 0,
                IsActive = true,
                IsHod = true,
                IsReviewer = false, // HODs are mentors, but not reviewer by default
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Lecturers.AddAsync(lecturer);
        }
        else
        {
            lecturer.FullName = user.FullName;
            lecturer.Avatar = user.Avatar;
            lecturer.CampusId = user.CampusId ?? 0;
            lecturer.IsActive = true;
            lecturer.IsHod = true;
            lecturer.UpdatedAt = DateTime.UtcNow;
            _context.Lecturers.Update(lecturer);
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteHodAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("HOD user not found.");

        if (user.Role?.RoleName != CampusConstants.Roles.HOD)
            throw new InvalidOperationException("User is not an HOD.");

        var cred = await _credentialRepository.GetByUserIdAsync(userId);
        if (cred != null)
        {
            await _credentialRepository.DeleteAsync(cred);
        }

        // Remove from Lecturers table as well
        var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == user.Email);
        if (lecturer != null)
        {
            _context.Lecturers.Remove(lecturer);
        }

        await _userRepository.DeleteAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateHodEmailAsync(int userId, string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("New email is required.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("HOD user not found.");

        if (user.Role?.RoleName != CampusConstants.Roles.HOD)
            throw new InvalidOperationException("User is not an HOD.");

        var existingWithEmail = await _userRepository.GetByEmailAsync(newEmail.Trim());
        if (existingWithEmail != null && existingWithEmail.UserId != userId)
            throw new InvalidOperationException($"Email '{newEmail}' is already used by another user.");

        user.Email = newEmail.Trim();
        await _userRepository.UpdateAsync(user);
    }
}
