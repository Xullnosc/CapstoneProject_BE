using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Services.AI.Text;

public sealed class TfIdfService : ITfIdfService
{
    private static readonly Regex TokenRegex = new("[a-zA-Z0-9]+", RegexOptions.Compiled);

    private static readonly HashSet<string> DefaultStopWords =
    [
        "a",
        "an",
        "and",
        "are",
        "as",
        "at",
        "be",
        "by",
        "for",
        "from",
        "in",
        "is",
        "it",
        "of",
        "on",
        "or",
        "that",
        "the",
        "this",
        "to",
        "was",
        "were",
        "with"
    ];

    public TfIdfModel BuildModel(IReadOnlyDictionary<string, string> documents, TfIdfOptions? options = null)
    {
        if (documents == null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        if (documents.Count == 0)
        {
            throw new ArgumentException("At least one document is required.", nameof(documents));
        }

        var effectiveOptions = options ?? new TfIdfOptions();
        ValidateOptions(effectiveOptions);

        var stopWords = effectiveOptions.StopWords ?? DefaultStopWords;
        var tokenizedDocuments = documents.ToDictionary(
            kvp => kvp.Key,
            kvp => Tokenize(kvp.Value, effectiveOptions.MinTokenLength, stopWords)
        );

        var documentFrequency = BuildDocumentFrequency(tokenizedDocuments);
        var filteredTerms = FilterTerms(documentFrequency, tokenizedDocuments.Count, effectiveOptions);

        var vocabulary = filteredTerms
            .OrderBy(t => t, StringComparer.Ordinal)
            .Select((term, idx) => new { term, idx })
            .ToDictionary(x => x.term, x => x.idx, StringComparer.Ordinal);

        var idfByTermId = BuildIdf(vocabulary, documentFrequency, tokenizedDocuments.Count);
        var vectors = BuildVectors(tokenizedDocuments, vocabulary, idfByTermId);

        return new TfIdfModel(vectors, vocabulary, idfByTermId);
    }

    public double CosineSimilarity(TfIdfDocumentVector left, TfIdfDocumentVector right)
    {
        if (left == null)
        {
            throw new ArgumentNullException(nameof(left));
        }

        if (right == null)
        {
            throw new ArgumentNullException(nameof(right));
        }

        if (left.NormalizedWeights.Count == 0 || right.NormalizedWeights.Count == 0)
        {
            return 0d;
        }

        var smaller = left.NormalizedWeights.Count <= right.NormalizedWeights.Count
            ? left.NormalizedWeights
            : right.NormalizedWeights;
        var larger = ReferenceEquals(smaller, left.NormalizedWeights)
            ? right.NormalizedWeights
            : left.NormalizedWeights;

        double dot = 0d;
        foreach (var (termId, weight) in smaller)
        {
            if (larger.TryGetValue(termId, out var otherWeight))
            {
                dot += weight * otherWeight;
            }
        }

        return dot;
    }

    public IReadOnlyList<TfIdfSimilarityScore> GetTopKSimilar(
        string sourceDocumentId,
        TfIdfModel model,
        int k,
        IEnumerable<string>? candidateDocumentIds = null,
        double minScore = 0d
    )
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (!model.Vectors.TryGetValue(sourceDocumentId, out var sourceVector))
        {
            throw new KeyNotFoundException($"Source document '{sourceDocumentId}' is not present in TF-IDF model.");
        }

        if (k <= 0)
        {
            return Array.Empty<TfIdfSimilarityScore>();
        }

        if (minScore < 0d || minScore > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(minScore), "minScore must be between 0 and 1.");
        }

        var candidateSet = candidateDocumentIds?.ToHashSet(StringComparer.Ordinal);
        var scores = new List<TfIdfSimilarityScore>();

        foreach (var (documentId, vector) in model.Vectors)
        {
            if (string.Equals(documentId, sourceDocumentId, StringComparison.Ordinal))
            {
                continue;
            }

            if (candidateSet != null && !candidateSet.Contains(documentId))
            {
                continue;
            }

            var score = CosineSimilarity(sourceVector, vector);
            if (score < minScore)
            {
                continue;
            }

            scores.Add(new TfIdfSimilarityScore(documentId, score));
        }

        return scores
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DocumentId, StringComparer.Ordinal)
            .Take(k)
            .ToArray();
    }

    private static void ValidateOptions(TfIdfOptions options)
    {
        if (options.MinTokenLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinTokenLength), "MinTokenLength must be at least 1.");
        }

        if (options.MinDocumentFrequency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinDocumentFrequency), "MinDocumentFrequency must be at least 1.");
        }

        if (options.MaxDocumentFrequencyRatio <= 0d || options.MaxDocumentFrequencyRatio > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxDocumentFrequencyRatio), "MaxDocumentFrequencyRatio must be in (0, 1].");
        }
    }

    private static IReadOnlyDictionary<string, int> BuildDocumentFrequency(
        IReadOnlyDictionary<string, IReadOnlyList<string>> tokenizedDocuments
    )
    {
        var frequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tokens in tokenizedDocuments.Values)
        {
            foreach (var term in tokens.Distinct(StringComparer.Ordinal))
            {
                if (!frequencies.TryAdd(term, 1))
                {
                    frequencies[term] += 1;
                }
            }
        }

        return frequencies;
    }

    private static IEnumerable<string> FilterTerms(
        IReadOnlyDictionary<string, int> documentFrequency,
        int totalDocuments,
        TfIdfOptions options
    )
    {
        var maxDf = Math.Max(1, (int)Math.Floor(totalDocuments * options.MaxDocumentFrequencyRatio));
        return documentFrequency
            .Where(kvp => kvp.Value >= options.MinDocumentFrequency && kvp.Value <= maxDf)
            .Select(kvp => kvp.Key);
    }

    private static IReadOnlyDictionary<int, double> BuildIdf(
        IReadOnlyDictionary<string, int> vocabulary,
        IReadOnlyDictionary<string, int> documentFrequency,
        int totalDocuments
    )
    {
        var idfByTermId = new Dictionary<int, double>(vocabulary.Count);
        foreach (var (term, termId) in vocabulary)
        {
            var df = documentFrequency[term];
            var idf = Math.Log((totalDocuments + 1d) / (df + 1d)) + 1d;
            idfByTermId[termId] = idf;
        }

        return idfByTermId;
    }

    private static IReadOnlyDictionary<string, TfIdfDocumentVector> BuildVectors(
        IReadOnlyDictionary<string, IReadOnlyList<string>> tokenizedDocuments,
        IReadOnlyDictionary<string, int> vocabulary,
        IReadOnlyDictionary<int, double> idfByTermId
    )
    {
        var vectors = new Dictionary<string, TfIdfDocumentVector>(tokenizedDocuments.Count, StringComparer.Ordinal);

        foreach (var (documentId, tokens) in tokenizedDocuments)
        {
            var termCounts = new Dictionary<int, int>();
            foreach (var token in tokens)
            {
                if (!vocabulary.TryGetValue(token, out var termId))
                {
                    continue;
                }

                if (!termCounts.TryAdd(termId, 1))
                {
                    termCounts[termId] += 1;
                }
            }

            var tfidfWeights = new Dictionary<int, double>(termCounts.Count);
            double squaredMagnitude = 0d;

            foreach (var (termId, count) in termCounts)
            {
                var tf = 1d + Math.Log(count);
                var weight = tf * idfByTermId[termId];
                tfidfWeights[termId] = weight;
                squaredMagnitude += weight * weight;
            }

            if (squaredMagnitude > 0d)
            {
                var magnitude = Math.Sqrt(squaredMagnitude);
                var keys = tfidfWeights.Keys.ToArray();
                foreach (var key in keys)
                {
                    tfidfWeights[key] /= magnitude;
                }
            }

            vectors[documentId] = new TfIdfDocumentVector(documentId, tfidfWeights);
        }

        return vectors;
    }

    private static IReadOnlyList<string> Tokenize(string text, int minTokenLength, IReadOnlySet<string> stopWords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>();
        foreach (Match match in TokenRegex.Matches(text.ToLowerInvariant()))
        {
            var token = match.Value;
            if (token.Length < minTokenLength)
            {
                continue;
            }

            if (stopWords.Contains(token))
            {
                continue;
            }

            tokens.Add(token);
        }

        return tokens;
    }
}