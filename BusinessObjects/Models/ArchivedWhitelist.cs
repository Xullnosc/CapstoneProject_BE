using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ArchivedWhitelist
{
    public int ArchivedWhitelistId { get; set; }

    public int OriginalWhitelistId { get; set; }

    public string? StudentCode { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public int? RoleId { get; set; }

    public bool IsReviewer { get; set; }

    public string? Avatar { get; set; }

    public string? Campus { get; set; }

    public int SemesterId { get; set; }

    public DateTime? ArchivedAt { get; set; }
}
