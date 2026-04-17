using System.Collections.Generic;

namespace Services.AI.Text;

public interface IHybridChunkingService
{
    IReadOnlyList<HybridTextChunk> ChunkDocument(
        string documentId,
        string content,
        HybridChunkingOptions? options = null
    );
}

public interface IChunkPreFilterService
{
    IReadOnlyList<CandidateChunkScore> SelectTopKCandidates(
        IReadOnlyList<HybridTextChunk> targetChunks,
        IReadOnlyList<CandidateChunk> candidateChunks,
        int k,
        double minSimilarity = 0d,
        TfIdfOptions? options = null
    );
}
