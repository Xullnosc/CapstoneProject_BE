using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

[Table("Whitelist")]
[Index("Email", Name = "UQ__Whitelis__A9D10534BDF4FDF3", IsUnique = true)]
public partial class Whitelist
{
    [Key]
    [Column("WhitelistID")]
    public int WhitelistId { get; set; }

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? StudentCode { get; set; }

    [StringLength(250)]
    public string? FullName { get; set; }

    [Column("RoleID")]
    public int? RoleId { get; set; }

    [StringLength(50)]
    public string? Campus { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AddedDate { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("Whitelists")]
    public virtual Role? Role { get; set; }
}
