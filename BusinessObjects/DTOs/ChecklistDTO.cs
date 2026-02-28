using System;

namespace BusinessObjects.DTOs;

public class ChecklistDTO
{
    public int ChecklistId { get; set; }
    public string Content { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class ChecklistCreateDTO
{
    public string Content { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public class ChecklistUpdateDTO
{
    public string Content { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
