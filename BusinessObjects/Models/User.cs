using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BusinessObjects.Models;

[Index("Email", Name = "UQ__Users__A9D1053404ECDA2B", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
}
