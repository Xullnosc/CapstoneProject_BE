using System.Collections.Generic;

namespace Services.AI.Text;

public sealed record TfIdfDocumentVector(
    string DocumentId,
    IReadOnlyDictionary<int, double> NormalizedWeights
);

public sealed record TfIdfSimilarityScore(string DocumentId, double Score);

public sealed class TfIdfModel
{
    public TfIdfModel(
        IReadOnlyDictionary<string, TfIdfDocumentVector> vectors,
        IReadOnlyDictionary<string, int> vocabulary,
        IReadOnlyDictionary<int, double> idfByTermId
    )
    {
        Vectors = vectors;
        Vocabulary = vocabulary;
        IdfByTermId = idfByTermId;
    }

    public IReadOnlyDictionary<string, TfIdfDocumentVector> Vectors { get; }

    public IReadOnlyDictionary<string, int> Vocabulary { get; }

    public IReadOnlyDictionary<int, double> IdfByTermId { get; }
}