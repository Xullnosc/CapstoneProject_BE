using System;
using System.Text.Json.Serialization;

namespace BusinessObjects.DTOs;

public class ChecklistDTO
{
    [JsonPropertyName("checklistId")]
    public int ChecklistId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class ChecklistCreateDTO
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

public class ChecklistUpdateDTO
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}
