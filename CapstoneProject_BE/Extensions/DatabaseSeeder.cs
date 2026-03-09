using System;
using System.Linq;
using BCrypt.Net;
using BusinessObjects;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace CapstoneProject_BE.Extensions;

public static class DatabaseSeeder
{
    /// <summary>
    /// Seed default Admin account on first run (if no Admin exists).
    /// </summary>
    public static void SeedDefaultAdmin(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FctmsContext>();

        var hasAdmin = context.Users
            .Include(u => u.Role)
            .Any(u => u.Role != null && u.Role.RoleName == CampusConstants.Roles.Admin);

        if (hasAdmin)
        {
            return;
        }

        var adminRole = context.Roles.FirstOrDefault(r => r.RoleName == CampusConstants.Roles.Admin);
        if (adminRole == null)
        {
            adminRole = new Role
            {
                RoleName = CampusConstants.Roles.Admin,
                Description = "System administrator"
            };
            context.Roles.Add(adminRole);
            context.SaveChanges();
        }

        var adminUser = new User
        {
            Email = "admin@system.local",
            FullName = "System Admin",
            RoleId = adminRole.RoleId,
            IsAuthorized = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        context.SaveChanges();

        var defaultUsername = "admin";
        var defaultPassword = "Admin@123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

        context.SystemUserCredentials.Add(new SystemUserCredential
        {
            UserId = adminUser.UserId,
            Username = defaultUsername,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        Console.WriteLine("=== Default Admin account created ===");
        Console.WriteLine($"Username: {defaultUsername}");
        Console.WriteLine($"Password: {defaultPassword}");
    }
}

