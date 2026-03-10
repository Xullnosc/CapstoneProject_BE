using System;

namespace BusinessObjects.Models;

public partial class ThesisHodDecision
{
    public long Id { get; set; }
    public string ThesisId { get; set; } = null!;
    public int HodId { get; set; }
    public string Decision { get; set; } = null!; // Pass | Fail
    public string? Note { get; set; }
    public DateTime DecidedAt { get; set; }
}

