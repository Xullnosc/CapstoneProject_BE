using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Services.AI.Text;

public sealed class HybridChunkingService : IHybridChunkingService
{
    private static readonly Regex SentenceSplitRegex = new(@"(?<=[\.!\?])\s+", RegexOptions.Compiled);

    private readonly ITfIdfService _tfIdfService;

    public HybridChunkingService(ITfIdfService tfIdfService)
    {
        _tfIdfService = tfIdfService;
    }

    public IReadOnlyList<HybridTextChunk> ChunkDocument(
        string documentId,
        string content,
        HybridChunkingOptions? options = null
    )
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<HybridTextChunk>();
        }

        var effective = options ?? new HybridChunkingOptions();
        ValidateOptions(effective);

        var sentences = SplitSentences(content);
        if (sentences.Count == 0)
        {
            return Array.Empty<HybridTextChunk>();
        }

        var sentenceTokenCounts = sentences.Select(EstimateTokens).ToArray();
        var chunks = new List<HybridTextChunk>();

        var start = 0;
        var chunkIndex = 0;

        while (start < sentences.Count)
        {
            var end = start;
            var tokenTotal = 0;

            while (end < sentences.Count)
            {
                var sentenceTokens = sentenceTokenCounts[end];
                var nextTotal = tokenTotal + sentenceTokens;

                if (nextTotal > effective.MaxChunkTokens && tokenTotal >= effective.MinChunkTokens)
                {
                    break;
                }

                tokenTotal = nextTotal;

                if (end + 1 < sentences.Count && tokenTotal >= effective.MinChunkTokens)
                {
                    var leftWindowText = string.Join(" ", sentences.Skip(Math.Max(start, end - effective.SimilarityWindowSentences + 1)).Take(effective.SimilarityWindowSentences));
                    var rightWindowText = string.Join(" ", sentences.Skip(end + 1).Take(effective.SimilarityWindowSentences));

                    if (HasTopicShift(leftWindowText, rightWindowText, effective.SimilarityDropThreshold))
                    {
                        break;
                    }
                }

                end += 1;
            }

            if (end == start)
            {
                end = Math.Min(start + 1, sentences.Count) - 1;
                tokenTotal = sentenceTokenCounts[end];
            }

            var chunkText = string.Join(" ", sentences.Skip(start).Take(end - start + 1));
            chunks.Add(
                new HybridTextChunk(
                    ChunkId: $"{documentId}-chunk-{chunkIndex:D4}",
                    Index: chunkIndex,
                    Text: chunkText,
                    StartSentenceIndex: start,
                    EndSentenceIndex: end,
                    ApproxTokenCount: tokenTotal
                )
            );

            chunkIndex += 1;

            var nextStart = end + 1 - effective.OverlapSentences;
            start = Math.Max(nextStart, start + 1);
        }

        return chunks;
    }

    private bool HasTopicShift(string leftText, string rightText, double threshold)
    {
        if (string.IsNullOrWhiteSpace(leftText) || string.IsNullOrWhiteSpace(rightText))
        {
            return false;
        }

        var model = _tfIdfService.BuildModel(
            new Dictionary<string, string>
            {
                ["left"] = leftText,
                ["right"] = rightText
            }
        );

        var similarity = _tfIdfService.CosineSimilarity(model.Vectors["left"], model.Vectors["right"]);
        return similarity < threshold;
    }

    private static int EstimateTokens(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return 0;
        }

        return sentence
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length;
    }

    private static IReadOnlyList<string> SplitSentences(string content)
    {
        return SentenceSplitRegex
            .Split(content)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private static void ValidateOptions(HybridChunkingOptions options)
    {
        if (options.MinChunkTokens < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinChunkTokens), "MinChunkTokens must be at least 1.");
        }

        if (options.MaxChunkTokens < options.MinChunkTokens)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxChunkTokens), "MaxChunkTokens must be greater than or equal to MinChunkTokens.");
        }

        if (options.OverlapSentences < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.OverlapSentences), "OverlapSentences must be at least 0.");
        }

        if (options.SimilarityWindowSentences < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.SimilarityWindowSentences), "SimilarityWindowSentences must be at least 1.");
        }

        if (options.SimilarityDropThreshold < 0d || options.SimilarityDropThreshold > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(options.SimilarityDropThreshold), "SimilarityDropThreshold must be between 0 and 1.");
        }
    }
}

public sealed class ChunkPreFilterService : IChunkPreFilterService
{
    private readonly ITfIdfService _tfIdfService;

    public ChunkPreFilterService(ITfIdfService tfIdfService)
    {
        _tfIdfService = tfIdfService;
    }

    public IReadOnlyList<CandidateChunkScore> SelectTopKCandidates(
        IReadOnlyList<HybridTextChunk> targetChunks,
        IReadOnlyList<CandidateChunk> candidateChunks,
        int k,
        double minSimilarity = 0d,
        TfIdfOptions? options = null
    )
    {
        if (targetChunks == null)
        {
            throw new ArgumentNullException(nameof(targetChunks));
        }

        if (candidateChunks == null)
        {
            throw new ArgumentNullException(nameof(candidateChunks));
        }

        if (k <= 0 || targetChunks.Count == 0 || candidateChunks.Count == 0)
        {
            return Array.Empty<CandidateChunkScore>();
        }

        var allDocuments = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var target in targetChunks)
        {
            allDocuments[target.ChunkId] = target.Text;
        }

        foreach (var candidate in candidateChunks)
        {
            allDocuments[candidate.ChunkId] = candidate.Text;
        }

        var model = _tfIdfService.BuildModel(allDocuments, options);
        var candidateById = candidateChunks.ToDictionary(x => x.ChunkId, StringComparer.Ordinal);
        var maxScores = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var targetChunk in targetChunks)
        {
            var similarities = _tfIdfService.GetTopKSimilar(
                targetChunk.ChunkId,
                model,
                k: candidateChunks.Count,
                candidateDocumentIds: candidateById.Keys,
                minScore: minSimilarity
            );

            foreach (var score in similarities)
            {
                if (!maxScores.TryGetValue(score.DocumentId, out var existing) || score.Score > existing)
                {
                    maxScores[score.DocumentId] = score.Score;
                }
            }
        }

        return maxScores
            .Select(x => new CandidateChunkScore(x.Key, candidateById[x.Key].ThesisId, x.Value))
            .OrderByDescending(x => x.Similarity)
            .ThenBy(x => x.ChunkId, StringComparer.Ordinal)
            .Take(k)
            .ToArray();
    }
}