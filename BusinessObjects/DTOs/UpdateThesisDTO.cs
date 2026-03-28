using Microsoft.AspNetCore.Http;

namespace BusinessObjects.DTOs
{
    public class UpdateThesisDTO
    {
        public IFormFile? File { get; set; }

        public string? Title { get; set; }
        public string? ShortDescription { get; set; }

        public string? ThesisNameEn { get; set; }
        public string? ThesisNameVi { get; set; }
        public string? Abbreviation { get; set; }
        public bool? IsFromEnterprise { get; set; }
        public string? EnterpriseName { get; set; }
        public bool? IsApplied { get; set; }
        public bool? IsAppUsed { get; set; }
    }
}
