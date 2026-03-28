using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.DTOs
{
    public class ForceAssignThesisDTO
    {
        [Required]
        public int TeamId { get; set; }
    }
}
