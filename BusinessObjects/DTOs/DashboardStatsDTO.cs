using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.DTOs
{
    public class DashboardStatsDTO
    {
        public int TotalUsers { get; set; }
        public int TotalTheses { get; set; }
        public int TotalTeams { get; set; }
        public int TotalSemesters { get; set; }
    }
}
