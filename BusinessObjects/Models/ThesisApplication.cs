using System;

namespace BusinessObjects.Models;

public partial class ThesisApplication
{
    public int Id { get; set; }

    public string ThesisId { get; set; } = null!;

    public int TeamId { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime? CreatedAt { get; set; }

    public virtual Thesis Thesis { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
