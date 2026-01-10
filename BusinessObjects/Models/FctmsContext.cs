using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

public partial class FctmsContext : DbContext
{
    public FctmsContext()
    {
    }

    public FctmsContext(DbContextOptions<FctmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Whitelist> Whitelists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A9160211C");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61605BBCDF23").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACE5FD21F4");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053478F2A74D").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Campus).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.IsAuthorized).HasDefaultValue(false);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StudentCode).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleID__403A8C7D");
        });

        modelBuilder.Entity<Whitelist>(entity =>
        {
            entity.HasKey(e => e.WhitelistId).HasName("PK__Whitelis__57EA36D6588B8F48");

            entity.ToTable("Whitelist");

            entity.HasIndex(e => e.Email, "UQ__Whitelis__A9D10534EF9A323F").IsUnique();

            entity.Property(e => e.WhitelistId).HasColumnName("WhitelistID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Campus).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StudentCode).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Whitelists)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Whitelist__RoleI__3B75D760");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
