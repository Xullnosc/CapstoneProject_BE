using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObjects.Models;

public partial class Lecturer
{
    public int LecturerId { get; set; }

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Avatar { get; set; }
    
    [NotMapped]
    public int? UserId { get; set; }
    
    [NotMapped]
    public string? Campus { get; set; }

    public bool IsHod { get; set; }

    public int CampusId { get; set; }

    public virtual Campus? CampusNavigation { get; set; }

    public bool IsActive { get; set; }

    public bool IsReviewer { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
