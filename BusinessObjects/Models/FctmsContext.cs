using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

public partial class FctmsContext : DbContext
{
    public FctmsContext(DbContextOptions<FctmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ArchivedTeam> ArchivedTeams { get; set; }

    public virtual DbSet<ArchivedWhitelist> ArchivedWhitelists { get; set; }

    public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Teaminvitation> Teaminvitations { get; set; }

    public virtual DbSet<Teammember> Teammembers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Whitelist> Whitelists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ArchivedTeam>(entity =>
        {
            entity.HasKey(e => e.ArchivedTeamId).HasName("PRIMARY");

            entity.ToTable("archived_teams");

            entity.HasIndex(e => e.OriginalTeamId, "IX_ArchivedTeam_Original");

            entity.HasIndex(e => e.SemesterId, "IX_ArchivedTeam_Semester");

            entity.Property(e => e.ArchivedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.JsonData).HasColumnType("json");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TeamCode).HasMaxLength(50);
            entity.Property(e => e.TeamName).HasMaxLength(100);
        });

        modelBuilder.Entity<ArchivedWhitelist>(entity =>
        {
            entity.HasKey(e => e.ArchivedWhitelistId).HasName("PRIMARY");

            entity.ToTable("archived_whitelists");

            entity.HasIndex(e => e.SemesterId, "IX_ArchivedWhitelist_Semester");

            entity.HasIndex(e => e.StudentCode, "IX_ArchivedWhitelist_Student");

            entity.Property(e => e.ArchivedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Campus).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.StudentCode).HasMaxLength(50);
        });

        modelBuilder.Entity<FlywaySchemaHistory>(entity =>
        {
            entity.HasKey(e => e.InstalledRank).HasName("PRIMARY");

            entity.ToTable("flyway_schema_history");

            entity.HasIndex(e => e.Success, "flyway_schema_history_s_idx");

            entity.Property(e => e.InstalledRank)
                .ValueGeneratedNever()
                .HasColumnName("installed_rank");
            entity.Property(e => e.Checksum).HasColumnName("checksum");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");
            entity.Property(e => e.InstalledBy)
                .HasMaxLength(100)
                .HasColumnName("installed_by");
            entity.Property(e => e.InstalledOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("installed_on");
            entity.Property(e => e.Script)
                .HasMaxLength(1000)
                .HasColumnName("script");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.Version)
                .HasMaxLength(50)
                .HasColumnName("version");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160E9B78D92").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PRIMARY");

            entity.ToTable("semesters");

            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.SemesterName).HasMaxLength(50);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PRIMARY");

            entity.ToTable("teams");

            entity.HasIndex(e => e.TeamCode, "TeamCode").IsUnique();

            entity.HasIndex(e => e.LeaderId, "idx_leader");

            entity.HasIndex(e => e.SemesterId, "idx_semester");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Insufficient'")
                .HasColumnType("enum('Insufficient','Pending','Qualified','Disbanded')");
            entity.Property(e => e.TeamAvatar).HasMaxLength(500);
            entity.Property(e => e.TeamCode).HasMaxLength(50);
            entity.Property(e => e.TeamName).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Leader).WithMany(p => p.Teams)
                .HasForeignKey(d => d.LeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_ibfk_2");

            entity.HasOne(d => d.Semester).WithMany(p => p.Teams)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_ibfk_1");
        });

        modelBuilder.Entity<Teaminvitation>(entity =>
        {
            entity.HasKey(e => e.InvitationId).HasName("PRIMARY");

            entity.ToTable("teaminvitations");

            entity.HasIndex(e => e.InvitedBy, "InvitedBy");

            entity.HasIndex(e => e.Status, "idx_status");

            entity.HasIndex(e => e.StudentId, "idx_student");

            entity.HasIndex(e => e.TeamId, "idx_team");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.RespondedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Accepted','Declined','Cancelled')");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.TeaminvitationInvitedByNavigations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teaminvitations_ibfk_3");

            entity.HasOne(d => d.Student).WithMany(p => p.TeaminvitationStudents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teaminvitations_ibfk_2");

            entity.HasOne(d => d.Team).WithMany(p => p.Teaminvitations)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("teaminvitations_ibfk_1");
        });

        modelBuilder.Entity<Teammember>(entity =>
        {
            entity.HasKey(e => e.TeamMemberId).HasName("PRIMARY");

            entity.ToTable("teammembers");

            entity.HasIndex(e => e.StudentId, "idx_student");

            entity.HasIndex(e => e.TeamId, "idx_team");

            entity.HasIndex(e => new { e.TeamId, e.StudentId }, "unique_team_student").IsUnique();

            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'Member'")
                .HasColumnType("enum('Leader','Member')");

            entity.HasOne(d => d.Student).WithMany(p => p.Teammembers)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teammembers_ibfk_2");

            entity.HasOne(d => d.Team).WithMany(p => p.Teammembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("teammembers_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleID");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105343BD5A87E").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Campus).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.IsAuthorized).HasDefaultValueSql("'0'");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StudentCode).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleID__5535A963");
        });

        modelBuilder.Entity<Whitelist>(entity =>
        {
            entity.HasKey(e => e.WhitelistId).HasName("PRIMARY");

            entity.ToTable("whitelist");

            entity.HasIndex(e => e.SemesterId, "FK_Whitelist_Semester");

            entity.HasIndex(e => e.RoleId, "IX_Whitelist_RoleID");

            entity.HasIndex(e => e.Email, "UQ__Whitelis__A9D10534BDF4FDF3").IsUnique();

            entity.Property(e => e.WhitelistId).HasColumnName("WhitelistID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Campus).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StudentCode).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Whitelists)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Whitelist__RoleI__5070F446");

            entity.HasOne(d => d.Semester).WithMany(p => p.Whitelists)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_Whitelist_Semester");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
