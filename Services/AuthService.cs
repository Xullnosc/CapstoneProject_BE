using AutoMapper;
using BusinessObjects;
using BusinessObjects.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Repositories;
using Services.DTOs;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AuthService(
            IUserRepository userRepository,
            IWhitelistRepository whitelistRepository,
      
            IMapper mapper,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _userRepository = userRepository;
            _whitelistRepository = whitelistRepository;
            _mapper = mapper;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<LoginResponseDTO> GoogleLoginAsync(LoginRequestDTO request)
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
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.IdToken); // "IdToken" in DTO actually holds Access Token
                    var response = await _httpClient.GetAsync(userInfoEndpoint);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[AUTH DEBUG] Google UserInfo failed: {response.StatusCode} - {errorBody}");
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
                        throw new UnauthorizedAccessException("Could not retrieve email from Google.");
                    }
                }
                catch (Exception ex) when (!(ex is UnauthorizedAccessException))
                {
                    Console.WriteLine($"[AUTH DEBUG] Exception validating Google token: {ex}");
                    throw new UnauthorizedAccessException($"Failed to validate Google token: {ex.Message}", ex);
                }

                // 0. Validate if Campus is valid
                if (string.IsNullOrEmpty(request.Campus) || !CampusConstants.All.Contains(request.Campus))
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
                else if (!string.Equals(whitelistEntry.Campus, request.Campus, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException($"Tài khoản của bạn thuộc cơ sở {whitelistEntry.Campus}. Vui lòng chọn đúng cơ sở khi đăng nhập.");
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
                        CreatedAt = DateTime.UtcNow
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
                        $"Bạn chưa được phân quyền vào hệ thống. Vui lòng liên hệ {supportEmail} / {supportPhone}");
                }

                // 5. Generate JWT Token
                var expireConfig = _configuration["Jwt:ExpireMinutes"];
                if (!int.TryParse(expireConfig, out int expireMinutes))
                {
                    Console.WriteLine($"[AUTH DEBUG] Invalid or missing Jwt:ExpireMinutes configuration: '{expireConfig}'. Using default 60 mins.");
                    expireMinutes = 60;
                }

                var jwtSettings = new JwtSettings
                {
                    Key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing in configuration."),
                    Issuer = _configuration["Jwt:Issuer"] ?? "FCTMS",
                    Audience = _configuration["Jwt:Audience"] ?? "FCTMS",
                    ExpireMinutes = expireMinutes
                };
                

                var isReviewer = whitelistEntry?.IsReviewer ?? false;

                var token = JwtTokenGenerator.GenerateToken(user, isReviewer, jwtSettings);

                // 6. Map to DTO and return
                var userInfo = _mapper.Map<UserInfoDTO>(user);

                return new LoginResponseDTO
                {
                    Token = token,
                    UserInfo = userInfo
                };
            }
            catch (Exception ex)
            {
                // Log crucial details to Azure Log Stream
                Console.WriteLine($"[CRITICAL AUTH ERROR] Exception in GoogleLoginAsync: {ex}");
                throw; // Rethrow to let AuthController handle the response
            }
        }
    }
}
