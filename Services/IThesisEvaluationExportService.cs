using BusinessObjects.DTOs;

namespace Services;

public interface IThesisEvaluationExportService
{
    Task<byte[]> GenerateWorkbookAsync(
        ReviewerSummarySheetRequestDTO request,
        CancellationToken cancellationToken = default
    );
}
