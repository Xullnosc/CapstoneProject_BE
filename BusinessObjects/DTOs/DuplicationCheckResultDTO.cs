using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class DuplicationCheckResultDTO
    {
        public string ThesisId { get; set; } = string.Empty;
        public string? ThesisTitle { get; set; }
        public int SemestersScanned { get; set; }
        public int CandidatesScanned { get; set; }
        public bool IsSuspicious { get; set; }
        /// <summary>True when at least one match was examined by the AI step.</summary>
        public bool AiEnriched { get; set; }
        public List<DuplicationMatchDTO> Matches { get; set; } = new();
    }

    public class DuplicationMatchDTO
    {
        public string CandidateThesisId { get; set; } = string.Empty;
        public string? CandidateTitle { get; set; }
        public int? CandidateSemesterId { get; set; }
        public string? CandidateSemesterCode { get; set; }
        public double MaxChunkSimilarity { get; set; }
        public double AverageTopChunkSimilarity { get; set; }
        public string SimilarityBand { get; set; } = string.Empty;
        /// <summary>AI semantic verdict. Null when AI was skipped or unavailable.</summary>
        public bool? AiConfirmedSuspicious { get; set; }
        /// <summary>One-sentence AI explanation for the verdict.</summary>
        public string? AiReasoning { get; set; }
        /// <summary>True when AI examination was not performed for this match.</summary>
        public bool AiSkipped { get; set; }
    }
}
