using System;

namespace BusinessObjects.Models;

public partial class Checklist
{
    public int ChecklistId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
