using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Whitelist
{
    public int WhitelistId { get; set; }

    public string Email { get; set; } = null!;

    public string? StudentCode { get; set; }

    public string? FullName { get; set; }

    public int? RoleId { get; set; }

    public string? Avatar { get; set; }

    public int CampusId { get; set; }

    public virtual Campus CampusNavigation { get; set; } = null!;

    public DateTime? AddedDate { get; set; }

    public int? SemesterId { get; set; }

    public virtual Role? Role { get; set; }

    public virtual Semester? Semester { get; set; }
}
