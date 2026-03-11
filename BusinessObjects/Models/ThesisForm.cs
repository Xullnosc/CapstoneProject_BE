using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisForm
{
    public int Id { get; set; }

    public string FileUrl { get; set; } = null!;

    public int VersionNumber { get; set; }

    public int UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ThesisFormHistory> ThesisFormHistories { get; set; } = new List<ThesisFormHistory>();

    public virtual User UploadedByNavigation { get; set; } = null!;
}
