using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Services.AI.Text;

var options = PlaygroundOptions.Parse(args);

if (!Directory.Exists(options.FolderPath))
{
    Console.Error.WriteLine($"Folder not found: {options.FolderPath}");
    return 1;
}

var docxFiles = Directory.GetFiles(options.FolderPath, "*.docx", SearchOption.TopDirectoryOnly);
if (docxFiles.Length == 0)
{
    Console.Error.WriteLine($"No .docx files found in: {options.FolderPath}");
    return 1;
}

var extractor = new DocxTextExtractor();
var tfIdfService = new TfIdfService();
var chunkingService = new HybridChunkingService(tfIdfService);

var chunkingOpts = new HybridChunkingOptions
{
    MinChunkTokens = options.MinChunkTokens,
    MaxChunkTokens = options.MaxChunkTokens,
    OverlapSentences = options.OverlapSentences,
    SimilarityWindowSentences = options.SimilarityWindowSentences,
    SimilarityDropThreshold = options.SimilarityDropThreshold
};

// --- Per-document extraction and chunking ---
var documents = new List<DocumentEntry>();

foreach (var filePath in docxFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
{
    var documentId = Path.GetFileNameWithoutExtension(filePath);
    Console.WriteLine($"Processing: {Path.GetFileName(filePath)}");

    var rawText = extractor.ExtractText(filePath);
    if (string.IsNullOrWhiteSpace(rawText))
    {
        Console.Error.WriteLine($"  Warning: no text extracted, skipping.");
        continue;
    }

    var chunks = chunkingService.ChunkDocument(
        documentId: documentId,
        content: rawText,
        options: chunkingOpts
    );

    if (chunks.Count == 0)
    {
        Console.Error.WriteLine($"  Warning: chunking produced no chunks, skipping.");
        continue;
    }

    documents.Add(new DocumentEntry(documentId, filePath, rawText, chunks));
    Console.WriteLine($"  Chunks: {chunks.Count}  |  Characters: {rawText.Length}");
}

if (documents.Count == 0)
{
    Console.Error.WriteLine("No documents could be processed.");
    return 1;
}

// --- Build one combined TF-IDF model across all chunks from all documents ---
var allChunkTexts = documents
    .SelectMany(d => d.Chunks)
    .ToDictionary(
        c => c.ChunkId,
        c => TfIdfPreprocessor.CleanChunkText(
            c.Text,
            isChunkZero: c.Index == 0,
            removeLikelyNames: options.RemoveLikelyNames,
            stripEmailsAndPhones: options.StripEmailsAndPhones
        ),
        StringComparer.Ordinal
    );

var stopWords = TfIdfPreprocessor.BuildStopWords(options.ExtraStopWords);
var termBoosts = options.EnableTermBoosts ? TfIdfPreprocessor.BuildTermBoosts() : null;

var tfIdfModel = tfIdfService.BuildModel(
    allChunkTexts,
    new TfIdfOptions
    {
        MinTokenLength = options.MinTokenLength,
        MinDocumentFrequency = options.MinDocumentFrequency,
        MaxDocumentFrequencyRatio = options.MaxDocumentFrequencyRatio,
        StopWords = stopWords,
        TermBoosts = termBoosts
    }
);

// --- Write reports ---
var outputDirectory = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(outputDirectory);

var summaryPath = Path.Combine(outputDirectory, "summary.txt");
var chunksPath = Path.Combine(outputDirectory, "chunks.txt");
var crossSimPath = Path.Combine(outputDirectory, "cross_similarity.txt");
var docSimPath = Path.Combine(outputDirectory, "document_similarity.txt");

File.WriteAllText(summaryPath, ReportBuilder.BuildSummary(options, documents, tfIdfModel), Encoding.UTF8);
File.WriteAllText(chunksPath, ReportBuilder.BuildChunkReport(documents, tfIdfModel, options.TopTermsPerChunk), Encoding.UTF8);
File.WriteAllText(crossSimPath, ReportBuilder.BuildCrossDocumentSimilarityReport(documents, tfIdfService, tfIdfModel, options.NeighborsPerChunk), Encoding.UTF8);
File.WriteAllText(docSimPath, ReportBuilder.BuildDocumentSimilarityMatrix(documents, tfIdfService, tfIdfModel), Encoding.UTF8);

Console.WriteLine();
Console.WriteLine($"Documents processed : {documents.Count}");
Console.WriteLine($"Total chunks        : {allChunkTexts.Count}");
Console.WriteLine($"Vocabulary size     : {tfIdfModel.Vocabulary.Count}");
Console.WriteLine($"Summary report      : {summaryPath}");
Console.WriteLine($"Chunk report        : {chunksPath}");
Console.WriteLine($"Cross-doc sim report: {crossSimPath}");
Console.WriteLine($"Doc similarity matrix: {docSimPath}");

return 0;

// ---------------------------------------------------------------------------
internal sealed record DocumentEntry(
    string DocumentId,
    string FilePath,
    string RawText,
    IReadOnlyList<HybridTextChunk> Chunks
);

internal sealed class PlaygroundOptions
{
    private const string DefaultFolderPath = @"C:\Users\Admin\Downloads\DEMO RESOURCE\Check_for_duplication";

    public string FolderPath { get; init; } = DefaultFolderPath;

    public int MinChunkTokens { get; init; } = 120;

    public int MaxChunkTokens { get; init; } = 220;

    public int OverlapSentences { get; init; } = 1;

    public int SimilarityWindowSentences { get; init; } = 3;

    public double SimilarityDropThreshold { get; init; } = 0.2d;

    public int MinTokenLength { get; init; } = 2;

    public int MinDocumentFrequency { get; init; } = 1;

    public double MaxDocumentFrequencyRatio { get; init; } = 0.95d;

    public bool StripEmailsAndPhones { get; init; } = true;

    public bool RemoveLikelyNames { get; init; } = true;

    public bool EnableTermBoosts { get; init; } = true;

    public string ExtraStopWords { get; init; } = string.Empty;

    public int TopTermsPerChunk { get; init; } = 12;

    public int NeighborsPerChunk { get; init; } = 3;

    public static PlaygroundOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index += 1)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = current[2..];
            var value = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[index + 1]
                : "true";

            values[key] = value;
        }

        return new PlaygroundOptions
        {
            FolderPath = GetString(values, "folder", DefaultFolderPath),
            MinChunkTokens = GetInt(values, "min-chunk-tokens", 120),
            MaxChunkTokens = GetInt(values, "max-chunk-tokens", 220),
            OverlapSentences = GetInt(values, "overlap-sentences", 1),
            SimilarityWindowSentences = GetInt(values, "similarity-window-sentences", 3),
            SimilarityDropThreshold = GetDouble(values, "similarity-drop-threshold", 0.2d),
            MinTokenLength = GetInt(values, "min-token-length", 2),
            MinDocumentFrequency = GetInt(values, "min-document-frequency", 1),
            MaxDocumentFrequencyRatio = GetDouble(values, "max-document-frequency-ratio", 0.95d),
            StripEmailsAndPhones = GetBool(values, "strip-emails-phones", true),
            RemoveLikelyNames = GetBool(values, "remove-likely-names", true),
            EnableTermBoosts = GetBool(values, "enable-term-boosts", true),
            ExtraStopWords = GetString(values, "extra-stop-words", string.Empty),
            TopTermsPerChunk = GetInt(values, "top-terms-per-chunk", 12),
            NeighborsPerChunk = GetInt(values, "neighbors-per-chunk", 3)
        };
    }

    private static string GetString(IDictionary<string, string> values, string key, string defaultValue)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;

    private static int GetInt(IDictionary<string, string> values, string key, int defaultValue)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;

    private static double GetDouble(IDictionary<string, string> values, string key, double defaultValue)
        => values.TryGetValue(key, out var raw) && double.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;

    private static bool GetBool(IDictionary<string, string> values, string key, bool defaultValue)
    {
        if (!values.TryGetValue(key, out var raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }
}

internal sealed class DocxTextExtractor
{
    public string ExtractText(string documentPath)
    {
        using var document = WordprocessingDocument.Open(documentPath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var paragraphs = body
            .Descendants<Paragraph>()
            .Select(paragraph => string.Concat(paragraph.Descendants<Text>().Select(text => text.Text)).Trim())
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph));

        return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
    }
}

internal static class TfIdfPreprocessor
{
    private static readonly Regex EmailPhoneRegex =
        new(@"\S+@\S+|\d{9,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AlphaNumericIdRegex =
        new(@"\b[A-Za-z]{1,4}\d{5,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LikelyPersonNameLineRegex =
        new(@"^\s*(?:\p{Lu}\p{L}+(?:[\s\-]+)){1,5}\p{Lu}\p{L}+\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ChunkZeroProjectAnchorRegex =
        new(@"\b3\s*(?:\.|\))\s*(?:register\s+content|capstone\s+project\s+name)\b|\b3\.1\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ChunkZeroBoilerplateLineRegex =
        new(
            @"(?im)^\s*(?:capstone\s+project\s+register|class\s*:|duration\s*time\s*:|profession\s*:|specialty\s*:|kinds\s+of\s+person\s+make\s+registers\s*:|register\s+information\s+for\s+supervisor|register\s+information\s+for\s+students|full\s+name|student\s+code|phone|e-?mail|title|role\s+in\s+group|supervisor\s*\d+|student\s*\d+|on\s+behalf\s+of\s+registers|sign\s+and\s+full\s+name|da\s+nang\s*,?)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

    private static readonly HashSet<string> PlaygroundStopWords =
    [
        "name",
        "phone",
        "title",
        "english",
        "vietnamese",
        "abbreviation",
        "website",
        "mobile",
        "application",
        "introduction",
        "objectives",
        "expected",
        "features",
        "feature",
        "role",
        "roles",
        "member",
        "members",
        "leader",
        "if",
        "have",
        "sign",
        "behalf",
        "da",
        "nang",
        "using",
        "use",
        "plan",
        "plans",
        "summary",
        "summarize",
        "summary",
        "brief",
        "technology",
        "algorithm",
        "front",
        "back",
        "end",
        "database",
        "version",
        "control",
        "code",
        "ci",
        "cd",
        "tools",
        "edu",
        "vn",
        "nguyễn",
        "trần",
        "lê",
        "phạm",
        "võ",
        "đặng",
        "huỳnh",
        "đinh",
        "bùi",
        "lưu",
        "mai",
        "ngô",
        "đỗ",
        "vũ",
        "2025",
        "2026",
        "ng",
        "nh",
        "th",
        "ph",
        "tr",
        "de"
    ];

    private static readonly Dictionary<string, double> DefaultTermBoosts =
        new(StringComparer.Ordinal)
        {
            ["task"] = 1.35,
            ["tasks"] = 1.35,
            ["sprint"] = 1.35,
            ["sprints"] = 1.35,
            ["backlog"] = 1.35,
            ["backlogs"] = 1.35,
            ["jwt"] = 1.60,
            ["rbac"] = 1.60,
            ["github"] = 1.45,
            ["gitlab"] = 1.30,
            ["firebase"] = 1.35,
            ["postgresql"] = 1.35,
            ["mongodb"] = 1.35,
            ["sql"] = 1.30,
            ["docker"] = 1.30,
            ["react"] = 1.25,
            ["reactjs"] = 1.25,
            ["nodejs"] = 1.25,
            ["nestjs"] = 1.25,
            ["asp"] = 1.25,
            ["net"] = 1.20,
            ["webrtc"] = 1.30,
            ["signalr"] = 1.30
        };

    public static string CleanChunkText(string text, bool isChunkZero, bool removeLikelyNames, bool stripEmailsAndPhones)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var cleaned = isChunkZero
            ? FocusChunkZeroOnProjectSemantics(text)
            : text;

        if (stripEmailsAndPhones)
        {
            cleaned = EmailPhoneRegex.Replace(cleaned, " ");
            cleaned = AlphaNumericIdRegex.Replace(cleaned, " ");
        }

        if (removeLikelyNames)
        {
            cleaned = LikelyPersonNameLineRegex.Replace(cleaned, " ");
        }

        return cleaned;
    }

    private static string FocusChunkZeroOnProjectSemantics(string text)
    {
        var match = ChunkZeroProjectAnchorRegex.Match(text);
        var focused = match.Success ? text[match.Index..] : text;

        focused = ChunkZeroBoilerplateLineRegex.Replace(focused, " ");
        focused = Regex.Replace(focused, @"\s{2,}", " ");

        return focused.Trim();
    }

    public static IReadOnlySet<string> BuildStopWords(string extraStopWords)
    {
        var merged = new HashSet<string>(PlaygroundStopWords, StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(extraStopWords))
        {
            var extra = extraStopWords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant());

            foreach (var token in extra)
            {
                merged.Add(token);
            }
        }

        return merged;
    }

    public static IReadOnlyDictionary<string, double> BuildTermBoosts()
        => DefaultTermBoosts;
}

internal static class ReportBuilder
{
    public static string BuildSummary(
        PlaygroundOptions options,
        IReadOnlyList<DocumentEntry> documents,
        TfIdfModel model)
    {
        var totalChunks = documents.Sum(d => d.Chunks.Count);
        var lines = new List<string>
        {
            "TF-IDF and Chunking Playground — Multi-Document",
            "",
            $"Folder: {options.FolderPath}",
            $"Documents processed: {documents.Count}",
            $"Total chunks: {totalChunks}",
            $"Vocabulary size: {model.Vocabulary.Count}",
            ""
        };

        lines.Add("Documents:");
        foreach (var doc in documents)
        {
            lines.Add($"  [{doc.DocumentId}]  chunks={doc.Chunks.Count}  chars={doc.RawText.Length}");
        }

        lines.Add("");
        lines.Add("Chunking options");
        lines.Add($"- MinChunkTokens: {options.MinChunkTokens}");
        lines.Add($"- MaxChunkTokens: {options.MaxChunkTokens}");
        lines.Add($"- OverlapSentences: {options.OverlapSentences}");
        lines.Add($"- SimilarityWindowSentences: {options.SimilarityWindowSentences}");
        lines.Add($"- SimilarityDropThreshold: {options.SimilarityDropThreshold}");
        lines.Add("");
        lines.Add("TF-IDF options");
        lines.Add($"- MinTokenLength: {options.MinTokenLength}");
        lines.Add($"- MinDocumentFrequency: {options.MinDocumentFrequency}");
        lines.Add($"- MaxDocumentFrequencyRatio: {options.MaxDocumentFrequencyRatio}");

        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildChunkReport(
        IReadOnlyList<DocumentEntry> documents,
        TfIdfModel model,
        int topTermsPerChunk)
    {
        var reverseVocabulary = model.Vocabulary.ToDictionary(pair => pair.Value, pair => pair.Key);
        var lines = new List<string>();

        foreach (var doc in documents)
        {
            lines.Add(new string('=', 80));
            lines.Add($"Document: {doc.DocumentId}");
            lines.Add($"File: {doc.FilePath}");
            lines.Add(new string('=', 80));

            foreach (var chunk in doc.Chunks)
            {
                lines.Add($"Chunk {chunk.Index}: {chunk.ChunkId}");
                lines.Add($"Sentences: {chunk.StartSentenceIndex} -> {chunk.EndSentenceIndex}");
                lines.Add($"Approx tokens: {chunk.ApproxTokenCount}");

                if (model.Vectors.TryGetValue(chunk.ChunkId, out var vector))
                {
                    var topTerms = vector.NormalizedWeights
                        .OrderByDescending(pair => pair.Value)
                        .Take(topTermsPerChunk)
                        .Select(pair => $"{reverseVocabulary[pair.Key]} ({pair.Value:F4})");

                    lines.Add($"Top terms: {string.Join(", ", topTerms)}");
                }
                else
                {
                    lines.Add("Top terms: <none>");
                }

                lines.Add("Text:");
                lines.Add(chunk.Text);
                lines.Add(new string('-', 80));
            }

            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// For each chunk, lists its most similar chunks from *other* documents only.
    /// </summary>
    public static string BuildCrossDocumentSimilarityReport(
        IReadOnlyList<DocumentEntry> documents,
        ITfIdfService tfIdfService,
        TfIdfModel model,
        int neighborsPerChunk)
    {
        // Build a lookup: chunkId -> documentId
        var chunkToDocument = documents
            .SelectMany(d => d.Chunks.Select(c => (c.ChunkId, d.DocumentId)))
            .ToDictionary(x => x.ChunkId, x => x.DocumentId, StringComparer.Ordinal);

        var lines = new List<string>
        {
            "Cross-Document Chunk Similarity Report",
            "(Only cross-document matches are shown — potential duplication)",
            ""
        };

        foreach (var doc in documents)
        {
            lines.Add(new string('=', 80));
            lines.Add($"Source document: {doc.DocumentId}");
            lines.Add(new string('=', 80));

            // Candidate chunk ids from all OTHER documents
            var candidateIds = documents
                .Where(d => d.DocumentId != doc.DocumentId)
                .SelectMany(d => d.Chunks.Select(c => c.ChunkId))
                .ToList();

            if (candidateIds.Count == 0)
            {
                lines.Add("No other documents to compare against.");
                lines.Add("");
                continue;
            }

            foreach (var chunk in doc.Chunks)
            {
                var neighbors = tfIdfService.GetTopKSimilar(
                    chunk.ChunkId, model, neighborsPerChunk,
                    candidateDocumentIds: candidateIds,
                    minScore: 0.05d
                );

                if (neighbors.Count == 0)
                {
                    continue;
                }

                lines.Add($"  Chunk {chunk.Index} [{chunk.ChunkId}]:");
                foreach (var neighbor in neighbors)
                {
                    var neighborDocId = chunkToDocument.TryGetValue(neighbor.DocumentId, out var nd) ? nd : "?";
                    lines.Add($"    -> [{neighborDocId}] {neighbor.DocumentId}  score={neighbor.Score:F4}");
                }
            }

            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Produces a document x document similarity matrix using the maximum
    /// pairwise chunk cosine similarity as the document-level score.
    /// </summary>
    public static string BuildDocumentSimilarityMatrix(
        IReadOnlyList<DocumentEntry> documents,
        ITfIdfService tfIdfService,
        TfIdfModel model)
    {
        var lines = new List<string>
        {
            "Document-Level Similarity Matrix",
            "(Score = max cosine similarity across all cross-document chunk pairs)",
            ""
        };

        for (var i = 0; i < documents.Count; i++)
        {
            for (var j = i + 1; j < documents.Count; j++)
            {
                var docA = documents[i];
                var docB = documents[j];

                var candidateIds = docB.Chunks.Select(c => c.ChunkId).ToList();

                double maxScore = 0d;
                double totalScore = 0d;
                var comparisonCount = 0;

                foreach (var chunkA in docA.Chunks)
                {
                    var neighbors = tfIdfService.GetTopKSimilar(
                        chunkA.ChunkId, model, 1,
                        candidateDocumentIds: candidateIds,
                        minScore: 0d
                    );

                    if (neighbors.Count > 0)
                    {
                        var score = neighbors[0].Score;
                        if (score > maxScore) maxScore = score;
                        totalScore += score;
                        comparisonCount++;
                    }
                }

                var avgScore = comparisonCount > 0 ? totalScore / comparisonCount : 0d;
                var similarityBand = GetSimilarityBand(maxScore);
                var similarityPercent = maxScore * 100d;

                lines.Add($"{docA.DocumentId}");
                lines.Add($"  <-> {docB.DocumentId}");
                lines.Add($"      Max similarity : {maxScore:F4}");
                lines.Add($"      Avg similarity : {avgScore:F4}");
                lines.Add($"      Similarity: {similarityBand} ({similarityPercent:F2}%)");
                lines.Add($"      Chunks compared: {comparisonCount} (from {docA.DocumentId})");
                lines.Add("");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetSimilarityBand(double score)
    {
        if (score >= 0.60d)
        {
            return "HIGH";
        }

        if (score >= 0.30d)
        {
            return "MEDIUM";
        }

        return "LOW";
    }
}