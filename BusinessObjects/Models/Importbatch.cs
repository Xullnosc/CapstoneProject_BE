using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Importbatch
{
    public int ImportBatchId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? AffectedSemesterId { get; set; }

    public int Version { get; set; }

    public string? Notes { get; set; }
}
