using System;
using System.Linq;
using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CapstoneProject_BE.Extensions;

public static class DatabaseSeeder
{
    private const string ConfigSection = "DefaultAdmin";

    /// <summary>
    /// Seed default Admin account on first run (if no Admin exists).
    /// Requires <c>DefaultAdmin:Password</c> (and other fields) in configuration; otherwise skips silently.
    /// </summary>
    public static void SeedDefaultAdmin(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseSeeder");

        var email = configuration[$"{ConfigSection}:Email"]?.Trim();
        var fullName = configuration[$"{ConfigSection}:FullName"]?.Trim();
        var username = configuration[$"{ConfigSection}:Username"]?.Trim();
        var password = configuration[$"{ConfigSection}:Password"];

        if (string.IsNullOrEmpty(password))
        {
            logger.LogDebug(
                "Default admin seed skipped: {Reason}.",
                "DefaultAdmin:Password is not set"
            );
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
        {
            logger.LogWarning(
                "Default admin seed skipped: Email and Username are required when Password is set."
            );
            return;
        }

        if (string.IsNullOrEmpty(fullName))
        {
            fullName = username;
        }

        var context = scope.ServiceProvider.GetRequiredService<FctmsContext>();

        var hasAdmin = context
            .Users.Include(u => u.Role)
            .Any(u => u.Role != null && u.Role.RoleName == CampusConstants.Roles.Admin);

        if (hasAdmin)
        {
            return;
        }

        var adminRole = context.Roles.FirstOrDefault(r =>
            r.RoleName == CampusConstants.Roles.Admin
        );
        if (adminRole == null)
        {
            adminRole = new Role
            {
                RoleName = CampusConstants.Roles.Admin,
                Description = "System administrator",
            };
            context.Roles.Add(adminRole);
            context.SaveChanges();
        }

        var adminUser = new User
        {
            Email = email,
            FullName = fullName,
            RoleId = adminRole.RoleId,
            IsAuthorized = true,
            CreatedAt = DateTime.UtcNow,
        };
        context.Users.Add(adminUser);
        context.SaveChanges();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        context.SystemUserCredentials.Add(
            new SystemUserCredential
            {
                UserId = adminUser.UserId,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
            }
        );
        context.SaveChanges();

        logger.LogInformation(
            "Default Admin account created (username: {Username}, email: {Email}).",
            username,
            email
        );
    }
}
