using System.Collections.Generic;

namespace Services.AI.Text;

public interface ITfIdfService
{
    TfIdfModel BuildModel(IReadOnlyDictionary<string, string> documents, TfIdfOptions? options = null);

    double CosineSimilarity(TfIdfDocumentVector left, TfIdfDocumentVector right);

    IReadOnlyList<TfIdfSimilarityScore> GetTopKSimilar(
        string sourceDocumentId,
        TfIdfModel model,
        int k,
        IEnumerable<string>? candidateDocumentIds = null,
        double minScore = 0d
    );
}