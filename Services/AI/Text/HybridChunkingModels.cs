using System.Collections.Generic;

namespace Services.AI.Text;

public sealed class HybridChunkingOptions
{
    public int MinChunkTokens { get; init; } = 500;

    public int MaxChunkTokens { get; init; } = 800;

    public int OverlapSentences { get; init; } = 2;

    public int SimilarityWindowSentences { get; init; } = 3;

    public double SimilarityDropThreshold { get; init; } = 0.2d;
}

public sealed record HybridTextChunk(
    string ChunkId,
    int Index,
    string Text,
    int StartSentenceIndex,
    int EndSentenceIndex,
    int ApproxTokenCount
);

public sealed record CandidateChunk(string ChunkId, string ThesisId, string Text);

public sealed record CandidateChunkScore(string ChunkId, string ThesisId, double Similarity);
