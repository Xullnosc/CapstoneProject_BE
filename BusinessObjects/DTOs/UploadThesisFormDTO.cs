using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class UploadThesisFormDTO
    {
        [Required(ErrorMessage = "SemesterId is required.")]
        public int SemesterId { get; set; }

        [Required(ErrorMessage = "File is required.")]
        public IFormFile File { get; set; } = null!;
    }
}
