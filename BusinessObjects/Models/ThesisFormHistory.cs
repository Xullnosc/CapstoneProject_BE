using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisFormHistory
{
    public int Id { get; set; }

    public int ThesisFormId { get; set; }

    public string FileUrl { get; set; } = null!;

    public int VersionNumber { get; set; }

    public int UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ThesisForm ThesisForm { get; set; } = null!;

    public virtual User UploadedByNavigation { get; set; } = null!;
}
