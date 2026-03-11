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
    public string? Note { get; set; }
}

