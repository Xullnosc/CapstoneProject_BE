using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Services.AI.Text;

var options = PlaygroundOptions.Parse(args);

if (!File.Exists(options.DocumentPath))
{
    Console.Error.WriteLine($"Document not found: {options.DocumentPath}");
    return 1;
}

var extractor = new DocxTextExtractor();
var rawText = extractor.ExtractText(options.DocumentPath);

if (string.IsNullOrWhiteSpace(rawText))
{
    Console.Error.WriteLine("No text could be extracted from the document.");
    return 1;
}

var tfIdfService = new TfIdfService();
var chunkingService = new HybridChunkingService(tfIdfService);

var chunks = chunkingService.ChunkDocument(
    documentId: options.DocumentId,
    content: rawText,
    options: new HybridChunkingOptions
    {
        MinChunkTokens = options.MinChunkTokens,
        MaxChunkTokens = options.MaxChunkTokens,
        OverlapSentences = options.OverlapSentences,
        SimilarityWindowSentences = options.SimilarityWindowSentences,
        SimilarityDropThreshold = options.SimilarityDropThreshold
    }
);

if (chunks.Count == 0)
{
    Console.Error.WriteLine("Chunking produced no chunks.");
    return 1;
}

var chunkDocuments = chunks.ToDictionary(chunk => chunk.ChunkId, chunk => chunk.Text, StringComparer.Ordinal);
var tfIdfModel = tfIdfService.BuildModel(
    chunkDocuments,
    new TfIdfOptions
    {
        MinTokenLength = options.MinTokenLength,
        MinDocumentFrequency = options.MinDocumentFrequency,
        MaxDocumentFrequencyRatio = options.MaxDocumentFrequencyRatio
    }
);

var outputDirectory = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(outputDirectory);

var summaryPath = Path.Combine(outputDirectory, "summary.txt");
var chunksPath = Path.Combine(outputDirectory, "chunks.txt");
var similarityPath = Path.Combine(outputDirectory, "similarity.txt");

File.WriteAllText(summaryPath, ReportBuilder.BuildSummary(options, rawText, chunks, tfIdfModel), Encoding.UTF8);
File.WriteAllText(chunksPath, ReportBuilder.BuildChunkReport(chunks, tfIdfModel, options.TopTermsPerChunk), Encoding.UTF8);
File.WriteAllText(similarityPath, ReportBuilder.BuildSimilarityReport(chunks, tfIdfService, tfIdfModel, options.NeighborsPerChunk), Encoding.UTF8);

Console.WriteLine($"Document: {options.DocumentPath}");
Console.WriteLine($"Characters extracted: {rawText.Length}");
Console.WriteLine($"Chunks created: {chunks.Count}");
Console.WriteLine($"Summary report: {summaryPath}");
Console.WriteLine($"Chunk report: {chunksPath}");
Console.WriteLine($"Similarity report: {similarityPath}");

return 0;

internal sealed class PlaygroundOptions
{
    private const string DefaultDocumentPath = @"C:\Users\chuon\Downloads\DEMO RESOURCE\DEMO RESOURCE\SP26-NguyenXuanLong.docx";

    public string DocumentPath { get; init; } = DefaultDocumentPath;

    public string DocumentId { get; init; } = "docx-sample";

    public int MinChunkTokens { get; init; } = 120;

    public int MaxChunkTokens { get; init; } = 220;

    public int OverlapSentences { get; init; } = 1;

    public int SimilarityWindowSentences { get; init; } = 3;

    public double SimilarityDropThreshold { get; init; } = 0.2d;

    public int MinTokenLength { get; init; } = 2;

    public int MinDocumentFrequency { get; init; } = 1;

    public double MaxDocumentFrequencyRatio { get; init; } = 0.95d;

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
            DocumentPath = GetString(values, "file", DefaultDocumentPath),
            DocumentId = GetString(values, "document-id", "docx-sample"),
            MinChunkTokens = GetInt(values, "min-chunk-tokens", 120),
            MaxChunkTokens = GetInt(values, "max-chunk-tokens", 220),
            OverlapSentences = GetInt(values, "overlap-sentences", 1),
            SimilarityWindowSentences = GetInt(values, "similarity-window-sentences", 3),
            SimilarityDropThreshold = GetDouble(values, "similarity-drop-threshold", 0.2d),
            MinTokenLength = GetInt(values, "min-token-length", 2),
            MinDocumentFrequency = GetInt(values, "min-document-frequency", 1),
            MaxDocumentFrequencyRatio = GetDouble(values, "max-document-frequency-ratio", 0.95d),
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

internal static class ReportBuilder
{
    public static string BuildSummary(
        PlaygroundOptions options,
        string rawText,
        IReadOnlyList<HybridTextChunk> chunks,
        TfIdfModel model)
    {
        var lines = new List<string>
        {
            "TF-IDF and Chunking Playground",
            "",
            $"Document path: {options.DocumentPath}",
            $"Document id: {options.DocumentId}",
            $"Characters extracted: {rawText.Length}",
            $"Chunks created: {chunks.Count}",
            $"Vocabulary size: {model.Vocabulary.Count}",
            "",
            "Chunking options",
            $"- MinChunkTokens: {options.MinChunkTokens}",
            $"- MaxChunkTokens: {options.MaxChunkTokens}",
            $"- OverlapSentences: {options.OverlapSentences}",
            $"- SimilarityWindowSentences: {options.SimilarityWindowSentences}",
            $"- SimilarityDropThreshold: {options.SimilarityDropThreshold}",
            "",
            "TF-IDF options",
            $"- MinTokenLength: {options.MinTokenLength}",
            $"- MinDocumentFrequency: {options.MinDocumentFrequency}",
            $"- MaxDocumentFrequencyRatio: {options.MaxDocumentFrequencyRatio}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildChunkReport(
        IReadOnlyList<HybridTextChunk> chunks,
        TfIdfModel model,
        int topTermsPerChunk)
    {
        var reverseVocabulary = model.Vocabulary.ToDictionary(pair => pair.Value, pair => pair.Key);
        var lines = new List<string>();

        foreach (var chunk in chunks)
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

        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildSimilarityReport(
        IReadOnlyList<HybridTextChunk> chunks,
        ITfIdfService tfIdfService,
        TfIdfModel model,
        int neighborsPerChunk)
    {
        var lines = new List<string>();

        foreach (var chunk in chunks)
        {
            lines.Add($"Source chunk: {chunk.ChunkId}");
            var neighbors = tfIdfService.GetTopKSimilar(chunk.ChunkId, model, neighborsPerChunk);

            if (neighbors.Count == 0)
            {
                lines.Add("No similar chunks found.");
                lines.Add(new string('-', 80));
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                lines.Add($"- {neighbor.DocumentId}: {neighbor.Score:F4}");
            }

            lines.Add(new string('-', 80));
        }

        return string.Join(Environment.NewLine, lines);
    }
}