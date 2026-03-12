using Microsoft.AspNetCore.Http;

namespace BusinessObjects.DTOs
{
    public class ReviewSubmissionDTO
    {
        public string Status { get; set; } = null!; // "Approve" | "Reject"
        public string? Comment { get; set; }
        public IFormFile? ReviewFile { get; set; }
    }
}
