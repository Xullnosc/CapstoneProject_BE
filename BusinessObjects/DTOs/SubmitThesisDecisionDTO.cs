namespace BusinessObjects.DTOs;

public class SubmitThesisDecisionDTO
{
    /// <summary>
    /// "Pass" | "Fail"
    /// </summary>
    public string Decision { get; set; } = null!;

    /// <summary>
    /// Required when Decision = "Fail"
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Optional review report file
    /// </summary>
    public Microsoft.AspNetCore.Http.IFormFile? ReviewFile { get; set; }
    public System.Collections.Generic.List<int>? CheckedChecklistIds { get; set; }
}

