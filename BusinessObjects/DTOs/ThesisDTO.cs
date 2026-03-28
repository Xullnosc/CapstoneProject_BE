using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class ThesisDTO
    {
        public string ThesisId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string? FileUrl { get; set; }
        public string? Status { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? UpDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? SemesterId { get; set; }
        public int? TeamId { get; set; }
        public int UserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerAvatar { get; set; }
        public string? MentorEmail1 { get; set; }
        public string? MentorEmail2 { get; set; }
        public string? ThesisNameEn { get; set; }
        public string? ThesisNameVi { get; set; }
        public string? Abbreviation { get; set; }
        public bool IsFromEnterprise { get; set; }
        public string? EnterpriseName { get; set; }
        public bool IsApplied { get; set; }
        public bool IsAppUsed { get; set; }

        public List<ReviewDTO> Reviews { get; set; } = new();
        public List<ThesisHistoryDTO>? Histories { get; set; }
    }
}
