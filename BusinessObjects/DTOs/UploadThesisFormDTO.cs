using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class UploadThesisFormDTO
    {
        [Required(ErrorMessage = "File is required.")]
        public IFormFile File { get; set; } = null!;
    }
}
