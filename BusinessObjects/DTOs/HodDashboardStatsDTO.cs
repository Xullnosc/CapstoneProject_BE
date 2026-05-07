using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class HodDashboardStatsDTO
    {
        public DashboardStatsDTO Summary { get; set; } = new();
        public string? CurrentSemesterCode { get; set; }
        public List<ThesisStatusCountDTO> ThesesByStatus { get; set; } = new();
        public List<SemesterTeamCountDTO> TeamsBySemester { get; set; } = new();
        public List<TeamStatusCountDTO> TeamsByStatus { get; set; } = new();
    }
}
