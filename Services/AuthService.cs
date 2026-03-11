using AutoMapper;
using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.Extensions.Configuration;
using Repositories;
using Services.DTOs;
using Services.Helpers;

namespace Services;

public class AuthService : IAuthService
{
    private const int DefaultRefreshExpireDays = 7;
    private readonly IUserRepository _userRepository;
    private readonly IWhitelistRepository _whitelistRepository;
    private readonly ISystemUserCredentialRepository _credentialRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IAccessLogRepository _accessLogRepository;

    public AuthService(
        IUserRepository userRepository,
        IWhitelistRepository whitelistRepository,
        ISystemUserCredentialRepository credentialRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IMapper mapper,
        IConfiguration configuration,
        HttpClient httpClient,
        IAccessLogRepository accessLogRepository
    )
    {
        _userRepository = userRepository;
        _whitelistRepository = whitelistRepository;
        _credentialRepository = credentialRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _mapper = mapper;
        _configuration = configuration;
        _httpClient = httpClient;
        _accessLogRepository = accessLogRepository;
    }

    public async Task<LoginResultDTO> GoogleLoginAsync(LoginRequestDTO request)
        {
            try
            {
                // 1. Validate Google Access Token
                string email;
                string fullName;
                string avatar;

                try
                {
                    var userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            request.IdToken
                        ); // "IdToken" in DTO actually holds Access Token
                    var response = await _httpClient.GetAsync(userInfoEndpoint);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(
                            $"[AUTH DEBUG] Google UserInfo failed: {response.StatusCode} - {errorBody}"
                        );
                        throw new UnauthorizedAccessException("Invalid Google Access Token.");
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    using (var doc = System.Text.Json.JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;
                        email = root.GetProperty("email").GetString() ?? "";
                        fullName = root.GetProperty("name").GetString() ?? "";
                        avatar = root.GetProperty("picture").GetString() ?? "";
                    }

                    if (string.IsNullOrEmpty(email))
                    {
                        throw new UnauthorizedAccessException(
                            "Could not retrieve email from Google."
                        );
                    }
                }
                catch (Exception ex) when (!(ex is UnauthorizedAccessException))
                {
                    Console.WriteLine($"[AUTH DEBUG] Exception validating Google token: {ex}");
                    throw new UnauthorizedAccessException(
                        $"Failed to validate Google token: {ex.Message}",
                        ex
                    );
                }

                // 0. Validate if Campus is valid
                if (
                    string.IsNullOrEmpty(request.Campus)
                    || !CampusConstants.All.Contains(request.Campus)
                )
                {
                    throw new UnauthorizedAccessException("Cơ sở không hợp lệ. Vui lòng chọn lại.");
                }

                // 2. Check if user is in whitelist
                var whitelistEntry = await _whitelistRepository.GetByEmailAsync(email);

                bool isAuthorized = false;
                if (whitelistEntry == null)
                {
                    isAuthorized = false;
                }
                else if (
                    !string.Equals(
                        whitelistEntry.Campus,
                        request.Campus,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        $"Tài khoản của bạn thuộc cơ sở {whitelistEntry.Campus}. Vui lòng chọn đúng cơ sở khi đăng nhập."
                    );
                }
                else
                {
                    isAuthorized = true;
                }

                int? roleId = whitelistEntry?.RoleId;
                string? studentCode = whitelistEntry?.StudentCode;
                string? campus = whitelistEntry?.Campus;

                if (whitelistEntry != null && !string.IsNullOrEmpty(whitelistEntry.FullName))
                {
                    fullName = whitelistEntry.FullName;
                }

                // 3. Check if user exists in Users table
                var user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        FullName = fullName,
                        Avatar = avatar,
                        RoleId = roleId,
                        StudentCode = studentCode,
                        Campus = campus,
                        IsAuthorized = isAuthorized,
                        LastLogin = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                    };

                    user = await _userRepository.AddAsync(user);
                }
                else
                {
                    user.FullName = fullName;
                    user.Avatar = avatar;
                    user.RoleId = roleId;
                    user.StudentCode = studentCode;
                    user.Campus = campus;
                    user.IsAuthorized = isAuthorized;
                    user.LastLogin = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);
                }

                // 4. Check authorization
                if (user.IsAuthorized == false)
                {
                    var supportEmail = _configuration["Support:Email"] ?? "N/A";
                    var supportPhone = _configuration["Support:Phone"] ?? "N/A";
                    throw new UnauthorizedAccessException(
                        $"Bạn chưa được phân quyền vào hệ thống. Vui lòng liên hệ {supportEmail} / {supportPhone}"
                    );
                }

                // 5. Generate JWT Token
                var jwtSettings = GetJwtSettings();

                var isReviewer = whitelistEntry?.IsReviewer ?? false;

                var accessToken = JwtTokenGenerator.GenerateToken(user, isReviewer, jwtSettings);
                var (refreshToken, refreshExpiresAt) = await CreateRefreshTokenAndSaveAsync(user.UserId);

                var userInfo = _mapper.Map<UserInfoDTO>(user);
                userInfo.IsReviewer = isReviewer;

                await _accessLogRepository.CreateLogAsync(new AccessLog
                {
                    UserId = user.UserId,
                    UserEmail = user.Email,
                    IpAddress = "N/A", // Handled by controller if needed, or left as N/A
                    Action = "Login (Google)",
                    IsSuccess = true,
                    Description = "User logged in via Google successfully"
                });

                return new LoginResultDTO
                {
                    AccessToken = accessToken,
                    UserInfo = userInfo,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshExpiresAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL AUTH ERROR] Exception in GoogleLoginAsync: {ex}");
                throw;
            }
        }

    public async Task<LoginResultDTO> CredentialLoginAsync(CredentialLoginRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Username and password are required.");

        var credential = await _credentialRepository.GetByUsernameAsync(request.Username.Trim());
        if (credential == null || credential.User?.Role == null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var roleName = credential.User.Role.RoleName;
        if (roleName != CampusConstants.Roles.HOD && roleName != CampusConstants.Roles.Admin)
            throw new UnauthorizedAccessException("Credential login is only allowed for HOD and Admin.");

        bool isValid = false;
        try
        {
            isValid = BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException ex)
        {
            // FALLBACK for development: If the stored hash is actually plain text, 
            // allow login and migrate it to a hashed version.
            if (request.Password == credential.PasswordHash)
            {
                Console.WriteLine($"[AUTH INFO] Migrating plain-text credential for user '{request.Username}' to BCrypt.");
                credential.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                credential.UpdatedAt = DateTime.UtcNow;
                await _credentialRepository.UpdateAsync(credential);
                isValid = true;
            }
            else
            {
                Console.WriteLine($"[AUTH ERROR] SaltParseException: {ex.Message}. User: {request.Username}");
                throw new UnauthorizedAccessException("Credential format is corrupted. Please contact support.");
            }
        }

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var user = credential.User;
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var jwtSettings = GetJwtSettings();
        var accessToken = JwtTokenGenerator.GenerateToken(user, false, jwtSettings);
        var (refreshToken, refreshExpiresAt) = await CreateRefreshTokenAndSaveAsync(user.UserId);

        var userInfo = _mapper.Map<UserInfoDTO>(user);
        
        await _accessLogRepository.CreateLogAsync(new AccessLog
        {
            UserId = user.UserId,
            UserEmail = user.Email,
            IpAddress = "N/A", // Handled by controller if needed
            Action = "Login (Credentials)",
            IsSuccess = true,
            Description = "User logged in via Credentials successfully"
        });

        return new LoginResultDTO
        {
            AccessToken = accessToken,
            UserInfo = userInfo,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }

    public async Task<RefreshResultDTO?> RefreshTokenAsync(string? refreshTokenFromCookie)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenFromCookie))
            return null;

        var tokenHash = RefreshTokenHelper.ComputeHash(refreshTokenFromCookie);
        var stored = await _refreshTokenRepository.GetValidByTokenHashAsync(tokenHash);
        if (stored == null)
            return null;

        await _refreshTokenRepository.RevokeByIdAsync(stored.Id);
        var user = await _userRepository.GetByIdAsync(stored.UserId);
        if (user == null || user.Role == null)
            return null;

        var jwtSettings = GetJwtSettings();
        var accessToken = JwtTokenGenerator.GenerateToken(user, user.Role.RoleName == CampusConstants.Roles.Lecturer && false, jwtSettings);
        var (newRefreshToken, newRefreshExpiresAt) = await CreateRefreshTokenAndSaveAsync(user.UserId);

        return new RefreshResultDTO
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAt = newRefreshExpiresAt
        };
    }

    public async Task RevokeRefreshTokenAsync(string? refreshTokenFromCookie)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenFromCookie))
            return;
        var tokenHash = RefreshTokenHelper.ComputeHash(refreshTokenFromCookie);
        var stored = await _refreshTokenRepository.GetValidByTokenHashAsync(tokenHash);
        if (stored != null)
        {
            await _refreshTokenRepository.RevokeByIdAsync(stored.Id);
            
            // Log logout
            var user = await _userRepository.GetByIdAsync(stored.UserId);
            if (user != null)
            {
                await _accessLogRepository.CreateLogAsync(new AccessLog
                {
                    UserId = user.UserId,
                    UserEmail = user.Email,
                    IpAddress = "N/A",
                    Action = "Logout",
                    IsSuccess = true,
                    Description = "User logged out successfully"
                });
            }
        }
    }

    private async Task<(string Token, DateTime ExpiresAt)> CreateRefreshTokenAndSaveAsync(int userId)
    {
        var refreshExpireDays = DefaultRefreshExpireDays;
        if (int.TryParse(_configuration["Jwt:RefreshExpireDays"], out var days) && days > 0)
        {
            refreshExpireDays = days;
        }

        var expiresAt = DateTime.UtcNow.AddDays(refreshExpireDays);
        var (token, tokenHash) = RefreshTokenHelper.GenerateTokenAndHash();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        });
        return (token, expiresAt);
    }

    private JwtSettings GetJwtSettings()
    {
        var expireMinutes = 60;
        var expireConfig = _configuration["Jwt:ExpireMinutes"];
        if (!string.IsNullOrWhiteSpace(expireConfig) && int.TryParse(expireConfig, out var parsed) && parsed > 0)
        {
            expireMinutes = parsed;
        }

        return new JwtSettings
        {
            Key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing."),
            Issuer = _configuration["Jwt:Issuer"] ?? "FCTMS",
            Audience = _configuration["Jwt:Audience"] ?? "FCTMS",
            ExpireMinutes = expireMinutes
        };
    }
}
