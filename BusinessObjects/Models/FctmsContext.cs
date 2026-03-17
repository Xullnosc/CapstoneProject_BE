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

    public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Teaminvitation> Teaminvitations { get; set; }

    public virtual DbSet<Teammember> Teammembers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Whitelist> Whitelists { get; set; }

    public virtual DbSet<Thesis> Theses { get; set; }

    public virtual DbSet<ThesisHistory> ThesisHistories { get; set; }


    public virtual DbSet<ThesisReview> ThesisReviews { get; set; }

    public virtual DbSet<ThesisHodDecision> ThesisHodDecisions { get; set; }

    public virtual DbSet<Checklist> Checklists { get; set; }

    public virtual DbSet<ThesisForm> ThesisForms { get; set; }

    public virtual DbSet<ThesisFormHistory> ThesisFormHistories { get; set; }
    public virtual DbSet<Lecturer> Lecturers { get; set; }
    public virtual DbSet<SystemUserCredential> SystemUserCredentials { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<AccessLog> AccessLogs { get; set; }

    public virtual DbSet<AccountDetail> AccountDetails { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ThesisForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("thesis_forms");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<ThesisFormHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("thesis_form_histories");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });


        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.ToTable("Notifications");

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }, "IX_Notifications_UserId_IsRead_CreatedAt");

            entity.HasIndex(e => e.CreatedAt, "IX_Notifications_CreatedAt");

            entity.Property(e => e.Type)
                .HasColumnType("enum('TeamInvitation','ThesisUpdate','MentorChange','SemesterDeadline','ChecklistUpdate','HODAction','SystemAnnouncement','FormSubmission')");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notifications_Users_UserId");
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

            entity.HasIndex(e => e.SemesterCode, "UQ_Semesters_SemesterCode").IsUnique();

            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.SemesterCode).HasMaxLength(50);
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

            entity.Property(e => e.MentorId).HasColumnName("MentorId");
            entity.Property(e => e.MentorId2).HasColumnName("MentorId2");

            entity.HasOne(d => d.Leader).WithMany(p => p.Teams)
                .HasForeignKey(d => d.LeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_ibfk_2");

            entity.HasOne(d => d.Semester).WithMany(p => p.Teams)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_ibfk_1");

            entity.HasOne(d => d.Mentor).WithMany()
                .HasForeignKey(d => d.MentorId)
                .HasConstraintName("FK_Teams_Users_MentorId");

            entity.HasOne(d => d.Mentor2).WithMany()
                .HasForeignKey(d => d.MentorId2)
                .HasConstraintName("FK_Teams_Users_MentorId2");
        });


        modelBuilder.Entity<ThesisReview>(entity =>
        {
            entity.HasKey(e => e.ThesisId).HasName("PRIMARY");
            entity.ToTable("thesis_reviews");

            entity.Property(e => e.ThesisId)
                .HasMaxLength(36)
                .HasColumnType("char(36)")
                .HasConversion(
                    v => Guid.Parse(v),
                    v => v.ToString()
                );

            entity.Property(e => e.Reviewer1Decision).HasColumnType("enum('Pass','Fail')");
            entity.Property(e => e.Reviewer2Decision).HasColumnType("enum('Pass','Fail')");
            
            entity.Property(e => e.Reviewer1Id).HasColumnName("Reviewer1Id");
            entity.Property(e => e.Reviewer2Id).HasColumnName("Reviewer2Id");

            entity.HasOne(d => d.Thesis).WithOne(p => p.ThesisReview)
                .HasForeignKey<ThesisReview>(d => d.ThesisId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Reviews_Thesis_Identity");

            entity.HasOne(d => d.Reviewer1).WithMany()
                .HasForeignKey(d => d.Reviewer1Id)
                .HasConstraintName("FK_Reviews_Lecturers_Reviewer1");

            entity.HasOne(d => d.Reviewer2).WithMany()
                .HasForeignKey(d => d.Reviewer2Id)
                .HasConstraintName("FK_Reviews_Lecturers_Reviewer2");
        });

        modelBuilder.Entity<ThesisHodDecision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("thesis_hod_decisions");
            entity.HasIndex(e => e.ThesisId, "UQ_HodDecision_Thesis").IsUnique();
            entity.Property(e => e.DecidedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ThesisId)
                .HasMaxLength(36)
                .HasColumnType("char(36)")
                .HasConversion(
                    v => Guid.Parse(v),
                    v => v.ToString()
                );
            entity.Property(e => e.Comment).HasColumnType("text");
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
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'Member'")
                .HasColumnType("varchar(20)");

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

            entity.HasOne(d => d.AccountDetail).WithOne(p => p.User)
                .HasForeignKey<AccountDetail>(d => d.UserId)
                .HasConstraintName("FK_AccountDetail_Users");
        });

        modelBuilder.Entity<AccountDetail>(entity =>
        {
            entity.HasKey(e => e.AccountDetailId).HasName("PRIMARY");

            entity.ToTable("account_detail");

            entity.HasIndex(e => e.UserId, "UQ_AccountDetail_UserId").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.GithubLink).HasMaxLength(255);
            entity.Property(e => e.LinkedInLink).HasMaxLength(255);
            entity.Property(e => e.FacebookLink).HasMaxLength(255);
            entity.Property(e => e.DateOfBirth).HasColumnType("date");
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Major).HasMaxLength(100);
            entity.Property(e => e.PersonalId).HasMaxLength(20);
            entity.Property(e => e.PlaceOfBirth).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
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

        modelBuilder.Entity<Thesis>(entity =>
        {
            entity.HasKey(e => e.ThesisId).HasName("PRIMARY");

            entity.ToTable("thesis");

            entity.HasIndex(e => e.UserId, "fk_thesis_userid");

            entity.Property(e => e.ThesisId)
                .HasMaxLength(36)
                .HasColumnType("char(36)")
                .HasConversion(
                    v => Guid.Parse(v),
                    v => v.ToString()
                )
                .HasDefaultValueSql("(uuid())");
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.ShortDescription).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'On Mentor Inviting'")
                .HasColumnType("enum('Published','Updated','Need Update','Reviewing','Rejected','Registered','Cancelled','On Mentor Inviting')");
            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false)
                .HasColumnName("IsLocked");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdateDate)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Theses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_thesis_userid");

            entity.HasOne(d => d.Semester).WithMany()
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("fk_thesis_semester");

            entity.HasOne(d => d.Team).WithMany()
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_Thesis_Teams_TeamId");

            entity.HasOne(d => d.Mentor1).WithMany()
                .HasForeignKey(d => d.MentorId1)
                .HasConstraintName("FK_Thesis_Lecturers_MentorId1");

            entity.HasOne(d => d.Mentor2).WithMany()
                .HasForeignKey(d => d.MentorId2)
                .HasConstraintName("FK_Thesis_Lecturers_MentorId2");
        });

        modelBuilder.Entity<ThesisHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("thesis_histories");

            entity.HasIndex(e => e.ThesisId, "FK_ThesisHistory_Thesis");
            entity.HasIndex(e => e.UploadedBy, "FK_ThesisHistory_User");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ThesisId)
                .HasMaxLength(36)
                .HasColumnType("char(36)")
                .HasConversion(
                    v => Guid.Parse(v),
                    v => v.ToString()
                );
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.VersionNumber).HasDefaultValueSql("1");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Thesis).WithMany(p => p.ThesisHistories)
                .HasForeignKey(d => d.ThesisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThesisHistory_Thesis");

            entity.HasOne(d => d.UploadedByUser).WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThesisHistory_User");
        });

        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.HasKey(e => e.ChecklistId).HasName("PRIMARY");

            entity.ToTable("checklists");

            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistId");
            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.HasKey(e => e.LecturerId).HasName("PRIMARY");
            entity.ToTable("lecturers");

            entity.HasIndex(e => e.Email, "UQ_Lecturers_Email").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.Campus).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.IsReviewer).HasColumnName("IsReviewer");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<SystemUserCredential>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");
            entity.ToTable("system_user_credentials");
            entity.HasIndex(e => e.Username, "UQ_SystemUserCredentials_Username").IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SystemUserCredentials_Users");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("refresh_tokens");
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt }, "IX_RefreshTokens_UserId_Expires");
            entity.Property(e => e.TokenHash).HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("access_logs");

            entity.HasIndex(e => e.UserId, "fk_accesslogs_userid");

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .HasColumnType("char(36)")
                .HasConversion(
                    v => Guid.Parse(v),
                    v => v.ToString()
                )
                .HasDefaultValueSql("(uuid())");

            entity.Property(e => e.IsSuccess)
                .HasDefaultValue(true)
                .HasColumnType("tinyint(1)");

            entity.Property(e => e.Description).HasColumnType("text");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime(6)");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AccessLogs_Users_UserId");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
