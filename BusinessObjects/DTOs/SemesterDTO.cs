using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class SemesterDTO
    {
        public int SemesterId { get; set; }
        public string SemesterCode { get; set; } = null!;
        public string SemesterName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool? IsActive { get; set; }
        public bool IsArchived { get; set; } // Flag to distinguish between Upcoming and officially Ended semesters
        
        // Include minimal team info or simplified list to avoid deep nesting
        // If the UI needs full team details, we can use a separate simplified DTO or stick to this if TeamDTO is already DTO-safe.
        // For "GetAllSemesters", usually just the list of Semesters is enough, or count of teams. 
        // But if the previous response included teams, we should check if they are needed.
        // Assuming we want to break the recursion but keep some info:
        // Optimized for Dashboard (List View) to avoid sending full Team list
        public int TeamCount { get; set; }
        public int WhitelistCount { get; set; } // Added for statistics
        public List<TeamSimpleDTO> Teams { get; set; } = new List<TeamSimpleDTO>();
        public List<WhitelistDTO> Whitelists { get; set; } = new List<WhitelistDTO>();
    }

    // Simplified DTOs to prevent circular references in Semester -> Team -> Leader -> Team...
    public class TeamSimpleDTO 
    {
        public int TeamId { get; set; }
        public string TeamCode { get; set; } = null!;
        public string TeamName { get; set; } = null!;
        public string? Status { get; set; }
        public int MemberCount { get; set; }
    }

    public class WhitelistDTO
    {
        public int WhitelistId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}
