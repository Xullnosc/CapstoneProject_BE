using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Lecturer
{
    public int LecturerId { get; set; }

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    public string? Campus { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
