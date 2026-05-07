using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.AI.Models;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using Repositories;
using Services.AI.Text;
using UglyToad.PdfPig;

namespace Services
{
    public sealed class ThesisDuplicationService : IThesisDuplicationService
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const int PreviousSemesterCount = 2;
        private const double SuspiciousThreshold = 0.40;
        private const double ReportThreshold = 0.15;
        private const int PreFilterTopK = 60;
        private const int TopKSimilarPerChunk = 5;
        private const int MaxExtractedChars = 30_000;

        // AI examination — only run on MEDIUM+ matches, cap candidates + chunk pairs
        private const double AiExaminationThreshold = 0.25;
        private const int AiMaxCandidatesToExamine = 5;
        private const int AiMaxChunkPairs = 3;
        private const int AiMaxChunkPreviewChars = 400;

        private static readonly TfIdfOptions CapstoneOptions = new()
        {
            DomainProfile = TfIdfOptions.DomainProfileCapstone
        };

        // ── Dependencies ─────────────────────────────────────────────────────────

        private readonly IThesisRepository _thesisRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ITfIdfService _tfidf;
        private readonly IHybridChunkingService _chunker;
        private readonly IChunkPreFilterService _preFilter;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIService _ai;
        private readonly ILogger<ThesisDuplicationService>? _logger;

        public ThesisDuplicationService(
            IThesisRepository thesisRepository,
            ISemesterRepository semesterRepository,
            ITfIdfService tfidf,
            IHybridChunkingService chunker,
            IChunkPreFilterService preFilter,
            IHttpClientFactory httpClientFactory,
            IAIService ai,
            ILogger<ThesisDuplicationService>? logger = null)
        {
            _thesisRepository = thesisRepository;
            _semesterRepository = semesterRepository;
            _tfidf = tfidf;
            _chunker = chunker;
            _preFilter = preFilter;
            _httpClientFactory = httpClientFactory;
            _ai = ai;
            _logger = logger;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public async Task<DuplicationCheckResultDTO> CheckAsync(
            string thesisId,
            CancellationToken cancellationToken = default)
        {
            // 1. Load source thesis
            var source = await _thesisRepository.GetThesisByIdAsync(thesisId)
                ?? throw new KeyNotFoundException($"Thesis '{thesisId}' not found.");

            // 2. Resolve the semester reference point
            int referenceSemesterId = source.SemesterId
                ?? (await _semesterRepository.GetCurrentSemesterAsync())?.SemesterId
                ?? 0;

            // 3. Get the 2 previous closed semesters
            var previousSemesters = referenceSemesterId > 0
                ? await _semesterRepository.GetPreviousClosedSemestersAsync(referenceSemesterId, PreviousSemesterCount)
                : new List<Semester>();

            if (previousSemesters.Count == 0)
            {
                // If no previous closed semester exists, still continue with current/reference semester.
                // This allows duplication checks against currently published theses.
            }

            var semesterIds = previousSemesters.Select(s => s.SemesterId).ToList();
            if (referenceSemesterId > 0 && !semesterIds.Contains(referenceSemesterId))
            {
                semesterIds.Add(referenceSemesterId);
            }
            var semesterCodeMap = previousSemesters.ToDictionary(s => s.SemesterId, s => s.SemesterCode);
            if (referenceSemesterId > 0 && !semesterCodeMap.ContainsKey(referenceSemesterId))
            {
                semesterCodeMap[referenceSemesterId] = source.Semester?.SemesterCode ?? string.Empty;
            }

            // 4. Load candidate theses (exclude source itself)
            //    Current semester: Published + Registered
            //    Previous semesters: Registered only
            var previousSemesterIds = previousSemesters.Select(s => s.SemesterId).ToList();
            var candidateResults = new List<Thesis>();

            if (referenceSemesterId > 0)
            {
                var current = await _thesisRepository.GetThesesBySemesterIdsAsync(
                    new[] { referenceSemesterId },
                    new[] { CampusConstants.ThesisStatus.Published, CampusConstants.ThesisStatus.Registered });
                candidateResults.AddRange(current);
            }

            if (previousSemesterIds.Count > 0)
            {
                var previous = await _thesisRepository.GetThesesBySemesterIdsAsync(
                    previousSemesterIds,
                    new[] { CampusConstants.ThesisStatus.Registered });
                candidateResults.AddRange(previous);
            }

            var candidates = candidateResults
                .Where(t => t.ThesisId != thesisId)
                .GroupBy(t => t.ThesisId)
                .Select(g => g.First())
                .ToList();

            if (candidates.Count == 0)
            {
                return CreateResult(thesisId, source.Title, previousSemesters.Count, 0, false, new List<DuplicationMatchDTO>(), false);
            }

            // 5. Extract + chunk source thesis
            var sourceText = await ExtractTextAsync(source.FileUrl, cancellationToken);
            if (string.IsNullOrWhiteSpace(sourceText))
                sourceText = BuildMetadataFallback(source);

            var sourceChunks = _chunker.ChunkDocument(thesisId, sourceText);
            if (sourceChunks.Count == 0)
            {
                return CreateResult(thesisId, source.Title, previousSemesters.Count, candidates.Count, false, new List<DuplicationMatchDTO>(), false);
            }

            // 6. Extract + chunk all candidates concurrently (cap concurrency to avoid I/O storm)
            var semaphore = new SemaphoreSlim(8, 8);
            var chunkTasks = candidates.Select(async c =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var text = await ExtractTextAsync(c.FileUrl, cancellationToken);
                    if (string.IsNullOrWhiteSpace(text))
                        text = BuildMetadataFallback(c);
                    var chunks = _chunker.ChunkDocument(c.ThesisId, text);
                    return (Thesis: c, Chunks: chunks);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var candidateChunkResults = await Task.WhenAll(chunkTasks);

            // 7. Pre-filter: select most relevant candidate chunks against source chunks
            var allCandidateChunks = candidateChunkResults
                .SelectMany(r => r.Chunks.Select(ch =>
                    new CandidateChunk(ch.ChunkId, r.Thesis.ThesisId, ch.Text)))
                .ToList();

            var topCandidateChunkScores = _preFilter.SelectTopKCandidates(
                sourceChunks,
                allCandidateChunks,
                k: PreFilterTopK,
                minSimilarity: 0.05,
                options: CapstoneOptions);

            var selectedChunkIds = new HashSet<string>(topCandidateChunkScores.Select(s => s.ChunkId));

            // 8. Build TF-IDF model: source chunks + selected candidate chunks
            var documents = new Dictionary<string, string>();
            foreach (var sc in sourceChunks)
                documents[sc.ChunkId] = sc.Text;

            foreach (var r in candidateChunkResults)
            {
                foreach (var ch in r.Chunks)
                {
                    if (selectedChunkIds.Contains(ch.ChunkId))
                        documents[ch.ChunkId] = ch.Text;
                }
            }

            if (documents.Count < 2)
            {
                return CreateResult(thesisId, source.Title, previousSemesters.Count, candidates.Count, false, new List<DuplicationMatchDTO>(), false);
            }

            var model = _tfidf.BuildModel(documents, CapstoneOptions);

            // 9. Score: for each source chunk get top-K similar, aggregate per candidate.
            //    Keep only bounded top values to reduce memory and sorting overhead.
            var candidateChunkById = allCandidateChunks.ToDictionary(c => c.ChunkId, StringComparer.Ordinal);
            var perCandidate = new Dictionary<string, CandidateScoreAccumulator>(StringComparer.Ordinal);

            foreach (var sc in sourceChunks)
            {
                if (!model.Vectors.ContainsKey(sc.ChunkId))
                    continue;

                var topSimilar = _tfidf.GetTopKSimilar(
                    sc.ChunkId,
                    model,
                    k: TopKSimilarPerChunk,
                    candidateDocumentIds: selectedChunkIds,
                    minScore: ReportThreshold);

                foreach (var match in topSimilar)
                {
                    if (!candidateChunkById.TryGetValue(match.DocumentId, out var candidateChunk))
                        continue;

                    var candidateThesisId = candidateChunk.ThesisId;

                    if (!perCandidate.TryGetValue(candidateThesisId, out var accumulator))
                    {
                        accumulator = new CandidateScoreAccumulator(TopKSimilarPerChunk, AiMaxChunkPairs);
                        perCandidate[candidateThesisId] = accumulator;
                    }

                    accumulator.AddScore(match.Score);
                    accumulator.AddPair(new ChunkPair(match.Score, sc.Text, candidateChunk.Text));
                }
            }

            // 9.5. AI examination — feed the top pre-filtered matches to the LLM for
            //      semantic validation before surfacing results in the UI.
            var thesisMap = candidates.ToDictionary(c => c.ThesisId, c => c);
            var aiVerdicts = new Dictionary<string, (bool? Suspicious, string? Reasoning)>(StringComparer.Ordinal);

            if (_ai.IsEnabled)
            {
                var candidatesForAi = perCandidate
                    .Where(kv => kv.Value.MaxScore >= AiExaminationThreshold)
                    .OrderByDescending(kv => kv.Value.MaxScore)
                    .Take(AiMaxCandidatesToExamine)
                    .ToList();

                foreach (var (candidateId, _) in candidatesForAi)
                {
                    var candidateThesis = thesisMap.TryGetValue(candidateId, out var ct) ? ct : null;
                    var topPairs = perCandidate.TryGetValue(candidateId, out var accumulator)
                        ? accumulator.TopPairs
                        : new List<ChunkPair>();

                    var verdict = await ExamineWithAiAsync(
                        source.Title ?? thesisId,
                        candidateThesis?.Title,
                        topPairs,
                        cancellationToken);

                    if (verdict.Suspicious.HasValue || !string.IsNullOrWhiteSpace(verdict.Reasoning))
                    {
                        aiVerdicts[candidateId] = verdict;
                    }
                }
            }

            // 10. Build matches DTO
            var matches = new List<DuplicationMatchDTO>();

            foreach (var (candidateId, accumulator) in perCandidate)
            {
                var maxScore = accumulator.MaxScore;
                if (maxScore < ReportThreshold)
                    continue;

                var avgTop = accumulator.AverageTopScores;
                var band = maxScore >= SuspiciousThreshold ? "HIGH"
                    : maxScore >= 0.25 ? "MEDIUM"
                    : "LOW";

                var candidateThesis = thesisMap.TryGetValue(candidateId, out var ct) ? ct : null;
                var semId = candidateThesis?.SemesterId;
                var hasAiVerdict = aiVerdicts.TryGetValue(candidateId, out var verdict);

                matches.Add(new DuplicationMatchDTO
                {
                    CandidateThesisId = candidateId,
                    CandidateTitle = candidateThesis?.Title,
                    CandidateSemesterId = semId,
                    CandidateSemesterCode = semId.HasValue && semesterCodeMap.TryGetValue(semId.Value, out var code) ? code : null,
                    MaxChunkSimilarity = Math.Round(maxScore, 4),
                    AverageTopChunkSimilarity = Math.Round(avgTop, 4),
                    SimilarityBand = band,
                    AiConfirmedSuspicious = hasAiVerdict ? verdict.Suspicious : null,
                    AiReasoning = hasAiVerdict ? verdict.Reasoning : null,
                    AiSkipped = !hasAiVerdict
                });
            }

            matches = matches.OrderByDescending(m => m.MaxChunkSimilarity).ToList();
            var isSuspicious = matches.Any(m => m.MaxChunkSimilarity >= SuspiciousThreshold);
            var aiEnriched = matches.Any(m => !m.AiSkipped);

            return CreateResult(
                thesisId,
                source.Title,
                previousSemesters.Count,
                candidates.Count,
                isSuspicious,
                matches,
                aiEnriched);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private async Task<string> ExtractTextAsync(string? fileUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return string.Empty;

            var urlPath = fileUrl.Contains('?') ? fileUrl[..fileUrl.IndexOf('?')] : fileUrl;
            var ext = Path.GetExtension(urlPath).ToLowerInvariant();
            var isDocx = ext is ".docx" or ".doc";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(25);

                using var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                if (!isDocx)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                    isDocx = contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
                          || contentType.Contains("msword", StringComparison.OrdinalIgnoreCase);
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
                using var memory = new MemoryStream();
                await responseStream.CopyToAsync(memory, ct);
                memory.Position = 0;

                string rawText = isDocx ? ExtractDocxText(memory) : ExtractPdfText(memory);
                return NormalizeWhitespace(rawText);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Duplication check: could not extract text from {Url}", fileUrl);
                return string.Empty;
            }
        }

        private static string ExtractDocxText(MemoryStream stream)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;

                var sb = new StringBuilder();
                foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                    if (sb.Length >= MaxExtractedChars * 2) break;
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractPdfText(MemoryStream stream)
        {
            try
            {
                using var doc = PdfDocument.Open(stream);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                {
                    sb.AppendLine(page.Text);
                    if (sb.Length >= MaxExtractedChars * 2) break;
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            bool lastWasSpace = false;
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasSpace) sb.Append(' ');
                    lastWasSpace = true;
                }
                else
                {
                    sb.Append(ch);
                    lastWasSpace = false;
                }
            }
            var result = sb.ToString().Trim();
            return result.Length > MaxExtractedChars ? result[..MaxExtractedChars] : result;
        }

        private static string BuildMetadataFallback(Thesis thesis)
            => $"{thesis.Title} {thesis.ThesisNameEn} {thesis.ThesisNameVi} {thesis.ShortDescription}";

        // ── AI examination helpers ────────────────────────────────────────────────

        private async Task<(bool? Suspicious, string? Reasoning)> ExamineWithAiAsync(
            string sourceTitle,
            string? candidateTitle,
            IReadOnlyList<ChunkPair> topPairs,
            CancellationToken ct)
        {
            if (topPairs.Count == 0)
                return (null, null);

            var prompt = BuildDuplicationAiPrompt(sourceTitle, candidateTitle, topPairs);
            try
            {
                var response = await _ai.GenerateAsync(
                    prompt: prompt,
                    systemPrompt: "You are an academic integrity reviewer for a university capstone project management system. Be concise and precise.",
                    temperature: 0.1f,
                    maxTokens: 256,
                    cancellationToken: ct);

                return ParseAiVerdict(response);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Duplication AI examination failed for a candidate match.");
                return (null, null);
            }
        }

        private static string BuildDuplicationAiPrompt(
            string sourceTitle,
            string? candidateTitle,
            IReadOnlyList<ChunkPair> pairs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Source thesis: \"{sourceTitle}\"");
            sb.AppendLine($"Candidate thesis: \"{candidateTitle ?? "(untitled)"}\"");
            sb.AppendLine();
            sb.AppendLine("The following passage pairs were identified as statistically similar by a TF-IDF model. Determine whether they indicate actual duplication or only share common academic language.");
            sb.AppendLine();

            for (var i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                sb.AppendLine($"[Pair {i + 1}]  TF-IDF similarity: {p.Score:P0}");
                sb.AppendLine($"Source    : {TruncatePreview(p.SourceText)}");
                sb.AppendLine($"Candidate : {TruncatePreview(p.CandidateText)}");
                sb.AppendLine();
            }

            sb.AppendLine("Respond with ONLY valid JSON:");
            sb.AppendLine("{");
            sb.AppendLine("  \"suspicious\": true | false,");
            sb.AppendLine("  \"reasoning\": \"one sentence explanation\"");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static (bool? Suspicious, string? Reasoning) ParseAiVerdict(string response)
        {
            try
            {
                var start = response.IndexOf('{');
                var end   = response.LastIndexOf('}');
                if (start < 0 || end <= start) return (null, null);

                using var doc  = JsonDocument.Parse(response[start..(end + 1)]);
                var root = doc.RootElement;

                bool? suspicious = root.TryGetProperty("suspicious", out var suspEl)
                    ? suspEl.ValueKind == JsonValueKind.True
                    : null;

                string? reasoning = root.TryGetProperty("reasoning", out var reasonEl)
                    ? reasonEl.GetString()
                    : null;

                return (suspicious, reasoning);
            }
            catch
            {
                return (null, null);
            }
        }

        private static DuplicationCheckResultDTO CreateResult(
            string thesisId,
            string? thesisTitle,
            int semestersScanned,
            int candidatesScanned,
            bool isSuspicious,
            List<DuplicationMatchDTO> matches,
            bool aiEnriched)
        {
            return new DuplicationCheckResultDTO
            {
                ThesisId = thesisId,
                ThesisTitle = thesisTitle,
                SemestersScanned = semestersScanned,
                CandidatesScanned = candidatesScanned,
                IsSuspicious = isSuspicious,
                AiEnriched = aiEnriched,
                Matches = matches
            };
        }

        private static string TruncatePreview(string text)
            => text.Length <= AiMaxChunkPreviewChars ? text : text[..AiMaxChunkPreviewChars] + "...";

        private sealed class CandidateScoreAccumulator
        {
            private readonly int _maxTopScores;
            private readonly int _maxTopPairs;
            private readonly List<double> _topScoresDescending;
            private readonly List<ChunkPair> _topPairsDescending;

            public CandidateScoreAccumulator(int maxTopScores, int maxTopPairs)
            {
                _maxTopScores = maxTopScores;
                _maxTopPairs = maxTopPairs;
                _topScoresDescending = new List<double>(maxTopScores);
                _topPairsDescending = new List<ChunkPair>(maxTopPairs);
            }

            public double MaxScore { get; private set; }

            public double AverageTopScores
                => _topScoresDescending.Count == 0 ? 0d : _topScoresDescending.Average();

            public List<ChunkPair> TopPairs => _topPairsDescending;

            public void AddScore(double score)
            {
                if (score > MaxScore)
                    MaxScore = score;

                InsertDescending(_topScoresDescending, score);
                if (_topScoresDescending.Count > _maxTopScores)
                    _topScoresDescending.RemoveAt(_topScoresDescending.Count - 1);
            }

            public void AddPair(ChunkPair pair)
            {
                InsertDescending(_topPairsDescending, pair, static p => p.Score);
                if (_topPairsDescending.Count > _maxTopPairs)
                    _topPairsDescending.RemoveAt(_topPairsDescending.Count - 1);
            }

            private static void InsertDescending(List<double> items, double value)
            {
                var index = items.FindIndex(existing => value > existing);
                if (index < 0)
                    items.Add(value);
                else
                    items.Insert(index, value);
            }

            private static void InsertDescending<T>(List<T> items, T value, Func<T, double> scoreSelector)
            {
                var score = scoreSelector(value);
                var index = items.FindIndex(existing => score > scoreSelector(existing));
                if (index < 0)
                    items.Add(value);
                else
                    items.Insert(index, value);
            }
        }
    }

    /// <summary>A scored chunk pair: one source chunk matched against one candidate chunk.</summary>
    internal sealed record ChunkPair(double Score, string SourceText, string CandidateText);
}
