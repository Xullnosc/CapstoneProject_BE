using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILogger<ThesisDuplicationService>? _logger;

        public ThesisDuplicationService(
            IThesisRepository thesisRepository,
            ISemesterRepository semesterRepository,
            ITfIdfService tfidf,
            IHybridChunkingService chunker,
            IChunkPreFilterService preFilter,
            IHttpClientFactory httpClientFactory,
            ILogger<ThesisDuplicationService>? logger = null)
        {
            _thesisRepository = thesisRepository;
            _semesterRepository = semesterRepository;
            _tfidf = tfidf;
            _chunker = chunker;
            _preFilter = preFilter;
            _httpClientFactory = httpClientFactory;
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
                return new DuplicationCheckResultDTO
                {
                    ThesisId = thesisId,
                    ThesisTitle = source.Title,
                    SemestersScanned = 0,
                    CandidatesScanned = 0,
                    IsSuspicious = false,
                    Matches = new List<DuplicationMatchDTO>()
                };
            }

            var semesterIds = previousSemesters.Select(s => s.SemesterId).ToList();
            var semesterCodeMap = previousSemesters.ToDictionary(s => s.SemesterId, s => s.SemesterCode);

            // 4. Load candidate theses (exclude source itself)
            var candidates = (await _thesisRepository.GetThesesBySemesterIdsAsync(semesterIds))
                .Where(t => t.ThesisId != thesisId)
                .ToList();

            if (candidates.Count == 0)
            {
                return new DuplicationCheckResultDTO
                {
                    ThesisId = thesisId,
                    ThesisTitle = source.Title,
                    SemestersScanned = previousSemesters.Count,
                    CandidatesScanned = 0,
                    IsSuspicious = false,
                    Matches = new List<DuplicationMatchDTO>()
                };
            }

            // 5. Extract + chunk source thesis
            var sourceText = await ExtractTextAsync(source.FileUrl, cancellationToken);
            if (string.IsNullOrWhiteSpace(sourceText))
                sourceText = BuildMetadataFallback(source);

            var sourceChunks = _chunker.ChunkDocument(thesisId, sourceText);
            if (sourceChunks.Count == 0)
            {
                return new DuplicationCheckResultDTO
                {
                    ThesisId = thesisId,
                    ThesisTitle = source.Title,
                    SemestersScanned = previousSemesters.Count,
                    CandidatesScanned = candidates.Count,
                    IsSuspicious = false,
                    Matches = new List<DuplicationMatchDTO>()
                };
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
                return new DuplicationCheckResultDTO
                {
                    ThesisId = thesisId,
                    ThesisTitle = source.Title,
                    SemestersScanned = previousSemesters.Count,
                    CandidatesScanned = candidates.Count,
                    IsSuspicious = false,
                    Matches = new List<DuplicationMatchDTO>()
                };
            }

            var model = _tfidf.BuildModel(documents, CapstoneOptions);

            // 9. Score: for each source chunk get top-K similar, aggregate per candidate
            // candidateThesisId -> list of top chunk similarity scores
            var perCandidateScores = new Dictionary<string, List<double>>(StringComparer.Ordinal);

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
                    // Resolve chunk → thesis
                    var candidateThesisId = allCandidateChunks
                        .FirstOrDefault(c => c.ChunkId == match.DocumentId)?.ThesisId;

                    if (candidateThesisId == null)
                        continue;

                    if (!perCandidateScores.TryGetValue(candidateThesisId, out var list))
                    {
                        list = new List<double>();
                        perCandidateScores[candidateThesisId] = list;
                    }
                    list.Add(match.Score);
                }
            }

            // 10. Build matches DTO
            var thesisMap = candidates.ToDictionary(c => c.ThesisId, c => c);
            var matches = new List<DuplicationMatchDTO>();

            foreach (var (candidateId, scores) in perCandidateScores)
            {
                var maxScore = scores.Max();
                if (maxScore < ReportThreshold)
                    continue;

                var avgTop = scores.OrderByDescending(s => s).Take(TopKSimilarPerChunk).Average();
                var band = maxScore >= SuspiciousThreshold ? "HIGH"
                    : maxScore >= 0.25 ? "MEDIUM"
                    : "LOW";

                var candidateThesis = thesisMap.TryGetValue(candidateId, out var ct) ? ct : null;
                var semId = candidateThesis?.SemesterId;

                matches.Add(new DuplicationMatchDTO
                {
                    CandidateThesisId = candidateId,
                    CandidateTitle = candidateThesis?.Title,
                    CandidateSemesterId = semId,
                    CandidateSemesterCode = semId.HasValue && semesterCodeMap.TryGetValue(semId.Value, out var code) ? code : null,
                    MaxChunkSimilarity = Math.Round(maxScore, 4),
                    AverageTopChunkSimilarity = Math.Round(avgTop, 4),
                    SimilarityBand = band
                });
            }

            matches = matches.OrderByDescending(m => m.MaxChunkSimilarity).ToList();
            var isSuspicious = matches.Any(m => m.MaxChunkSimilarity >= SuspiciousThreshold);

            return new DuplicationCheckResultDTO
            {
                ThesisId = thesisId,
                ThesisTitle = source.Title,
                SemestersScanned = previousSemesters.Count,
                CandidatesScanned = candidates.Count,
                IsSuspicious = isSuspicious,
                Matches = matches
            };
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
    }
}
