using System;

namespace BusinessObjects.Models;

public partial class Checklist
{
    public int ChecklistId { get; set; }

    public string Content { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public DateTime? CreatedAt { get; set; }
}
