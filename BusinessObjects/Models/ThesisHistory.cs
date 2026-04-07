using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisHistory
{
    public int Id { get; set; }

    public string ThesisId { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string? Description { get; set; }

    public int UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual User UploadedByUser { get; set; } = null!;
}
