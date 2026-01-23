using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ArchivedTeam
{
    public int ArchivedTeamId { get; set; }

    public int OriginalTeamId { get; set; }

    public string? TeamCode { get; set; }

    public string? TeamName { get; set; }

    public int SemesterId { get; set; }
    
    public int LeaderId { get; set; }

    public string? Status { get; set; }

    public DateTime? ArchivedAt { get; set; }
    
    // Storing JSON snapshot as string for simplicity in EF Core, user can parse if needed
    public string? JsonData { get; set; }
}
