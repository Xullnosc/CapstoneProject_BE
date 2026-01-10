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

        public AuthService(
            IUserRepository userRepository,
            IWhitelistRepository whitelistRepository,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _whitelistRepository = whitelistRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDTO> GoogleLoginAsync(LoginRequestDTO request)
        {
            // 1. Validate Google IdToken
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var googleClientId = _configuration["Google:ClientId"];
                if (string.IsNullOrEmpty(googleClientId))
                {
                    throw new InvalidOperationException("Google Client ID is not configured in appsettings.json");
                }

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    // Tạm thời comment để test với Google Playground 
                    // Audience = new[] { googleClientId } 
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                throw new UnauthorizedAccessException($"Invalid Google token: {ex.Message}", ex);
            }

            var email = payload.Email;
            var fullName = payload.Name;
            var avatar = payload.Picture;

            // 0. Validate if Campus is valid
            if (string.IsNullOrEmpty(request.Campus) || !CampusConstants.All.Contains(request.Campus))
            {
                throw new UnauthorizedAccessException("Cơ sở không hợp lệ. Vui lòng chọn lại.");
            }

            // 2. Check if user is in whitelist
            var whitelistEntry = await _whitelistRepository.GetByEmailAsync(email);

            bool isAuthorized = false;
            // Kiểm tra nếu không có trong Whitelist HOẶC sai Campus đã chọn
            if (whitelistEntry == null)
            {
                isAuthorized = false;
            }
            else if (!string.Equals(whitelistEntry.Campus, request.Campus, StringComparison.OrdinalIgnoreCase))
            {
                // Trường hợp có trong Whitelist nhưng chọn sai Campus ở Dropdown
                throw new UnauthorizedAccessException($"Tài khoản của bạn thuộc cơ sở {whitelistEntry.Campus}. Vui lòng chọn đúng cơ sở khi đăng nhập.");
            }
            else
            {
                isAuthorized = true;
            }

            int? roleId = whitelistEntry?.RoleId;
            string? studentCode = whitelistEntry?.StudentCode;
            string? campus = whitelistEntry?.Campus;

            // If whitelist entry has FullName, use it; otherwise use Google's name
            if (whitelistEntry != null && !string.IsNullOrEmpty(whitelistEntry.FullName))
            {
                fullName = whitelistEntry.FullName;
            }

            // 3. Check if user exists in Users table
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Email = email,
                    FullName = fullName,
                    Avatar = avatar,
                    RoleId = roleId,
                    StudentCode = studentCode,
                    Campus = campus,
                    IsAuthorized = isAuthorized,
                    LastLogin = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                user = await _userRepository.AddAsync(user);
            }
            else
            {
                // Update existing user
                user.FullName = fullName;
                user.Avatar = avatar;
                user.RoleId = roleId;
                user.StudentCode = studentCode;
                user.Campus = campus;
                user.IsAuthorized = isAuthorized;
                user.LastLogin = DateTime.Now;

                await _userRepository.UpdateAsync(user);
            }

            // 4. Check authorization
            if (user.IsAuthorized == false)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa được phân quyền vào hệ thống. Vui lòng liên hệ longnx6@fe.edu.vn / 0905 764750");
            }

            // 5. Generate JWT Token
            var jwtSettings = new JwtSettings
            {
                Key = _configuration["Jwt:Key"]!,
                Issuer = _configuration["Jwt:Issuer"]!,
                Audience = _configuration["Jwt:Audience"]!,
                ExpireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"]!)
            };

            var token = JwtTokenGenerator.GenerateToken(user, jwtSettings);

            // 6. Map to DTO and return
            var userInfo = _mapper.Map<UserInfoDTO>(user);

            return new LoginResponseDTO
            {
                Token = token,
                UserInfo = userInfo
            };
        }
    }
}
