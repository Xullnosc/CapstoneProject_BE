using FluentAssertions;
using Services.AI.Text;

namespace FCTMS.Tests.Services.AI.Text;

public class HybridChunkingServiceTests
{
    private readonly ITfIdfService _tfIdfService = new TfIdfService();

    [Fact]
    public void ChunkDocument_ShouldSplitIntoMultipleChunks_WhenMaxTokensReached()
    {
        var service = new HybridChunkingService(_tfIdfService);
        var content = string.Join(
            " ",
            Enumerable.Repeat("This sentence has five tokens.", 8)
        );

        var chunks = service.ChunkDocument(
            "doc-1",
            content,
            new HybridChunkingOptions
            {
                MinChunkTokens = 10,
                MaxChunkTokens = 15,
                OverlapSentences = 1,
                SimilarityWindowSentences = 2,
                SimilarityDropThreshold = 0.01d
            }
        );

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Select(c => c.ChunkId).Should().OnlyHaveUniqueItems();
        chunks.Should().OnlyContain(c => c.ApproxTokenCount > 0);
    }

    [Fact]
    public void SelectTopKCandidates_ShouldReturnMostSimilarCandidateChunks()
    {
        var preFilter = new ChunkPreFilterService(_tfIdfService);
        var targetChunks = new[]
        {
            new HybridTextChunk("target-0001", 0, "nlp model for thesis duplication detection", 0, 0, 7)
        };

        var candidates = new[]
        {
            new CandidateChunk("cand-1", "thesis-a", "thesis duplication detection with nlp model"),
            new CandidateChunk("cand-2", "thesis-b", "smart irrigation using iot and cloud dashboard"),
            new CandidateChunk("cand-3", "thesis-c", "duplicate thesis checking and plagiarism detection")
        };

        var top = preFilter.SelectTopKCandidates(targetChunks, candidates, k: 2);

        top.Should().HaveCount(2);
        top[0].ChunkId.Should().Be("cand-1");
        top.Any(x => x.ChunkId == "cand-2").Should().BeFalse();
    }
}
