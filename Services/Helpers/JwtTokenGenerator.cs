using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.IdentityModel.Tokens;

namespace Services.Helpers
{
    public class JwtTokenGenerator
    {
        public static string GenerateToken(User user, bool isReviewer, JwtSettings jwtSettings)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };

            // Add role claim if user has a role
            if (user.Role != null && !string.IsNullOrEmpty(user.Role.RoleName))
            {
                claims.Add(new Claim("role", user.Role.RoleName));

                // Add campus_id if user is NOT an Admin or Super Admin
                if (user.Role.RoleName != CampusConstants.Roles.Admin && user.CampusId > 0)
                {
                    claims.Add(new Claim("campus_id", user.CampusId.ToString()));
                }
            }
            
            if (isReviewer)
            {
                claims.Add(new Claim("IsReviewer", "true"));
            }

            // MySQL `datetime` often truncates sub-second precision; normalize to seconds
            // so token stamp matches the value reloaded from DB during validation.
            // IMPORTANT: MySQL `datetime` comes back as Kind=Unspecified; treat it as UTC (do not convert),
            // otherwise ToUniversalTime() may apply a local-time shift and break the stamp match.
            var lastLoginRaw = user.LastLogin ?? DateTime.UtcNow;
            var lastLoginUtc = lastLoginRaw.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(lastLoginRaw, DateTimeKind.Utc)
                : lastLoginRaw.ToUniversalTime();

            var normalizedUtc = new DateTime(
                lastLoginUtc.Year,
                lastLoginUtc.Month,
                lastLoginUtc.Day,
                lastLoginUtc.Hour,
                lastLoginUtc.Minute,
                lastLoginUtc.Second,
                DateTimeKind.Utc
            );
            var sessionStamp = normalizedUtc.Ticks.ToString();
            claims.Add(new Claim("session_stamp", sessionStamp));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class JwtSettings
    {
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpireMinutes { get; set; }
    }
}
