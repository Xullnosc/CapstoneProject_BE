using Microsoft.AspNetCore.Http;

namespace BusinessObjects.DTOs
{
    public class UpdateThesisDTO
    {
        public IFormFile? File { get; set; }

        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
    }
}
