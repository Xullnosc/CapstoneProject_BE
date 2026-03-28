namespace BusinessObjects.AI.Models;

public sealed class AIUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>Rough USD estimate based on known public pricing. Zero when not calculable.</summary>
    public decimal EstimatedCostUsd { get; init; }
}
