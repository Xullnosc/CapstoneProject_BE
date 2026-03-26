using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ThesisReviewChecklistResult
{
    public long EventId { get; set; }
    public int ChecklistId { get; set; }
    public bool IsChecked { get; set; }

    public virtual ThesisReviewEvent Event { get; set; } = null!;
    public virtual Checklist Checklist { get; set; } = null!;
}
