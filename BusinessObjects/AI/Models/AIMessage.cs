namespace BusinessObjects.AI.Models;

/// <param name="Role">Who authored this message.</param>
/// <param name="Content">Plain-text content of the message.</param>
public sealed record AIMessage(AIMessageRole Role, string Content);
