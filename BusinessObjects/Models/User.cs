using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

[Index("Email", Name = "UQ__Users__A9D105343BD5A87E", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? StudentCode { get; set; }

    [StringLength(250)]
    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    [Column("RoleID")]
    public int? RoleId { get; set; }

    public bool? IsAuthorized { get; set; }

    [StringLength(50)]
    public string? Campus { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastLogin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role? Role { get; set; }
}
