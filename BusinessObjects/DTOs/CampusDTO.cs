using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class CampusDTO
    {
        public int CampusId { get; set; }
        public string CampusCode { get; set; } = null!;
        public string CampusName { get; set; } = null!;
        public bool IsActive { get; set; }
        public List<HodSummaryDTO> Hods { get; set; } = new List<HodSummaryDTO>();
    }

    public class CreateCampusDTO
    {
        public string CampusCode { get; set; } = null!;
        public string CampusName { get; set; } = null!;
    }

    public class UpdateCampusDTO
    {
        public string? CampusName { get; set; }
        public bool? IsActive { get; set; }
    }

    public class HodSummaryDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
