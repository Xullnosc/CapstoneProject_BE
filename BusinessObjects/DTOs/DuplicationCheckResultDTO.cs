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
    }
}
