using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisHistory
{
    public int Id { get; set; }

    public Guid ThesisId { get; set; }

    public string FileUrl { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string? Note { get; set; }

    public int UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual User UploadedByNavigation { get; set; } = null!;
}
