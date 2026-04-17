using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Services.AI.Text;

public sealed class TfIdfService : ITfIdfService
{
    private static readonly HashSet<string> SupportedProfiles =
    [
        TfIdfOptions.DomainProfileGeneral,
        TfIdfOptions.DomainProfileCapstone
    ];

    private static readonly Regex TokenRegex = new("[\\p{L}\\p{Nd}]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> DefaultStopWords =
    [
        "a",
        "about",
        "above",
        "after",
        "again",
        "against",
        "all",
        "also",
        "am",
        "an",
        "and",
        "any",
        "are",
        "around",
        "as",
        "at",
        "because",
        "been",
        "before",
        "being",
        "be",
        "below",
        "between",
        "both",
        "by",
        "can",
        "could",
        "did",
        "do",
        "does",
        "doing",
        "done",
        "during",
        "each",
        "few",
        "further",
        "for",
        "from",
        "had",
        "has",
        "have",
        "having",
        "he",
        "her",
        "here",
        "hers",
        "herself",
        "him",
        "himself",
        "his",
        "how",
        "i",
        "if",
        "into",
        "in",
        "is",
        "itself",
        "it",
        "just",
        "me",
        "more",
        "most",
        "my",
        "myself",
        "no",
        "nor",
        "not",
        "now",
        "off",
        "of",
        "on",
        "once",
        "only",
        "other",
        "our",
        "ours",
        "ourselves",
        "out",
        "over",
        "own",
        "or",
        "same",
        "she",
        "should",
        "so",
        "some",
        "such",
        "that",
        "than",
        "their",
        "theirs",
        "them",
        "themselves",
        "then",
        "there",
        "these",
        "the",
        "this",
        "those",
        "through",
        "too",
        "to",
        "under",
        "until",
        "up",
        "very",
        "was",
        "we",
        "what",
        "when",
        "where",
        "which",
        "while",
        "who",
        "whom",
        "why",
        "will",
        "would",
        "were",
        "you",
        "your",
        "yours",
        "yourself",
        "yourselves",
        "with"
    ];

    private static readonly IReadOnlyDictionary<string, double> DefaultTermBoosts =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["task"] = 1.20,
            ["tasks"] = 1.20,
            ["sprint"] = 1.20,
            ["sprints"] = 1.20,
            ["backlog"] = 1.20,
            ["backlogs"] = 1.20,
            ["jwt"] = 1.30,
            ["rbac"] = 1.30,
            ["oauth"] = 1.25,
            ["github"] = 1.20,
            ["gitlab"] = 1.20,
            ["docker"] = 1.20,
            ["kubernetes"] = 1.20,
            ["postgresql"] = 1.20,
            ["mongodb"] = 1.20,
            ["redis"] = 1.20,
            ["webrtc"] = 1.20,
            ["signalr"] = 1.20,
            ["graphql"] = 1.20,
            ["microservice"] = 1.20,
            ["microservices"] = 1.20
        };

    private static readonly HashSet<string> CapstoneStopWords =
    [
        "class",
        "duration",
        "specialty",
        "lecturer",
        "student",
        "students",
        "supervisor",
        "phone",
        "email",
        "title",
        "english",
        "vietnamese",
        "abbreviation",
        "introduction",
        "objectives",
        "expected",
        "feature",
        "features",
        "website",
        "mobile",
        "summary",
        "summarize",
        "technology",
        "algorithm",
        "version",
        "control",
        "code",
        "management",
        "ci",
        "cd",
        "tools",
        "leader",
        "member",
        "members",
        "semester",
        "dashboard"
    ];

    private static readonly IReadOnlyDictionary<string, double> CapstoneTermBoosts =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["scrum"] = 1.20,
            ["sprint"] = 1.25,
            ["sprints"] = 1.25,
            ["backlog"] = 1.25,
            ["backlogs"] = 1.25,
            ["retrospective"] = 1.20,
            ["retrospectives"] = 1.20,
            ["thesis"] = 1.15,
            ["jwt"] = 1.30,
            ["rbac"] = 1.30,
            ["mentor"] = 1.20,
            ["weekly"] = 1.15,
            ["velocity"] = 1.20
        };

    private static readonly IReadOnlyDictionary<string, double> CapstoneTechTermDownweights =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["react"] = 0.80,
            ["reactjs"] = 0.80,
            ["angular"] = 0.80,
            ["vue"] = 0.80,
            ["vuejs"] = 0.80,
            ["flutter"] = 0.80,
            ["nextjs"] = 0.80,
            ["next"] = 0.85,
            ["android"] = 0.85,
            ["ios"] = 0.85,
            ["node"] = 0.80,
            ["nodejs"] = 0.80,
            ["express"] = 0.85,
            ["spring"] = 0.85,
            ["dotnet"] = 0.85,
            ["net"] = 0.85,
            ["csharp"] = 0.85,
            ["java"] = 0.85,
            ["python"] = 0.85,
            ["javascript"] = 0.85,
            ["typescript"] = 0.85,
            ["php"] = 0.85,
            ["sql"] = 0.85,
            ["mysql"] = 0.85,
            ["postgres"] = 0.85,
            ["postgresql"] = 0.85,
            ["mongodb"] = 0.85,
            ["redis"] = 0.85,
            ["firebase"] = 0.85,
            ["aws"] = 0.85,
            ["azure"] = 0.85,
            ["gcp"] = 0.85,
            ["docker"] = 0.85,
            ["kubernetes"] = 0.85,
            ["api"] = 0.80,
            ["apis"] = 0.80,
            ["rest"] = 0.80,
            ["restful"] = 0.80,
            ["backend"] = 0.80,
            ["frontend"] = 0.80,
            ["fullstack"] = 0.80,
            ["database"] = 0.80,
            ["databases"] = 0.80,
            ["framework"] = 0.80,
            ["frameworks"] = 0.80,
            ["module"] = 0.80,
            ["modules"] = 0.80,
            ["platform"] = 0.80,
            ["cloud"] = 0.80,
            ["devops"] = 0.80,
            ["deployment"] = 0.80,
            ["hosting"] = 0.80,
            ["application"] = 0.75,
            ["applications"] = 0.75,
            ["system"] = 0.75,
            ["software"] = 0.75
        };

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

        var normalizedProfile = NormalizeProfile(effectiveOptions.DomainProfile);
        var stopWords = effectiveOptions.StopWords ?? BuildDefaultStopWords(normalizedProfile);
        var termBoosts = effectiveOptions.TermBoosts ?? BuildDefaultTermBoosts(normalizedProfile);
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
        var vectors = BuildVectors(tokenizedDocuments, vocabulary, idfByTermId, termBoosts);

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

        if (!string.IsNullOrWhiteSpace(options.DomainProfile))
        {
            var profile = options.DomainProfile.ToLowerInvariant();
            if (!SupportedProfiles.Contains(profile))
            {
                throw new ArgumentException(
                    $"Unsupported domain profile '{options.DomainProfile}'. Supported values: {string.Join(", ", SupportedProfiles)}",
                    nameof(options.DomainProfile)
                );
            }
        }
    }

    private static string NormalizeProfile(string? profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            return TfIdfOptions.DomainProfileGeneral;
        }

        return profile.ToLowerInvariant();
    }

    private static IReadOnlySet<string> BuildDefaultStopWords(string profile)
    {
        if (string.Equals(profile, TfIdfOptions.DomainProfileCapstone, StringComparison.Ordinal))
        {
            var merged = new HashSet<string>(DefaultStopWords, StringComparer.Ordinal);
            foreach (var word in CapstoneStopWords)
            {
                merged.Add(word);
            }

            return merged;
        }

        return DefaultStopWords;
    }

    private static IReadOnlyDictionary<string, double> BuildDefaultTermBoosts(string profile)
    {
        if (!string.Equals(profile, TfIdfOptions.DomainProfileCapstone, StringComparison.Ordinal))
        {
            return DefaultTermBoosts;
        }

        var merged = new Dictionary<string, double>(DefaultTermBoosts, StringComparer.Ordinal);
        foreach (var (key, value) in CapstoneTermBoosts)
        {
            if (!merged.TryGetValue(key, out var existing) || value > existing)
            {
                merged[key] = value;
            }
        }

        foreach (var (key, value) in CapstoneTechTermDownweights)
        {
            if (!merged.TryGetValue(key, out var existing) || value < existing)
            {
                merged[key] = value;
            }
        }

        return merged;
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
        IReadOnlyDictionary<int, double> idfByTermId,
        IReadOnlyDictionary<string, double>? termBoosts
    )
    {
        var vectors = new Dictionary<string, TfIdfDocumentVector>(tokenizedDocuments.Count, StringComparer.Ordinal);
        var boostsByTermId = termBoosts == null
            ? new Dictionary<int, double>()
            : termBoosts
                .Where(kvp => kvp.Value > 0d && vocabulary.ContainsKey(kvp.Key))
                .ToDictionary(kvp => vocabulary[kvp.Key], kvp => kvp.Value);

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

                if (boostsByTermId.TryGetValue(termId, out var boost))
                {
                    weight *= boost;
                }

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