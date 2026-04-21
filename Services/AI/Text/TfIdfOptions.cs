using System.Collections.Generic;

namespace Services.AI.Text;

public sealed class TfIdfOptions
{
    public const string DomainProfileGeneral = "general";

    public const string DomainProfileCapstone = "capstone";

    public int MinTokenLength { get; init; } = 2;

    public int MinDocumentFrequency { get; init; } = 1;

    public double MaxDocumentFrequencyRatio { get; init; } = 0.95d;

    public IReadOnlySet<string>? StopWords { get; init; }

    public IReadOnlyDictionary<string, double>? TermBoosts { get; init; }

    public string? DomainProfile { get; init; }
}