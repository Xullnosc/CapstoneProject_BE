using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Repositories;

namespace Services;

public class ThesisEvaluationExportService : IThesisEvaluationExportService
{
    private const string SummarySheetName = "Bảng tổng hợp";
    private const string ThesisInfoSheetName = "Thông tin đề tài ";
    private static readonly Color HeaderBackgroundColor = ColorTranslator.FromHtml("#70AD47");
    private static readonly Color ReviewerHeaderBackgroundColor = ColorTranslator.FromHtml("#FFE1CC");
    private static readonly Color InfoEditColumnColor = ColorTranslator.FromHtml("#FF0000");
    private static readonly Color KhongDuDieuKienColor = ColorTranslator.FromHtml("#FFCCCC");
    private readonly ILecturerRepository _lecturerRepository;
    private readonly IThesisRepository _thesisRepository;
    private readonly IChecklistRepository _checklistRepository;

    public ThesisEvaluationExportService(
        ILecturerRepository lecturerRepository,
        IThesisRepository thesisRepository,
        IChecklistRepository checklistRepository
    )
    {
        _lecturerRepository = lecturerRepository;
        _thesisRepository = thesisRepository;
        _checklistRepository = checklistRepository;
    }

    public async Task<byte[]> GenerateWorkbookAsync(
        ReviewerSummarySheetRequestDTO request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviewers = await _lecturerRepository.GetReviewersAsync();
        var reviewerList = reviewers
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .DistinctBy(r => GetEmailPrefix(r.Email))
            .ToList();
        var reviewerEmails = reviewerList.Select(r => r.Email).ToList();
        var allTheses = await _thesisRepository.GetThesesForEvaluationExportAsync();
        // Filter by semester in-memory using the already-loaded Team navigation.
        // This handles both direct Thesis.SemesterId and team-linked semester assignment.
        IEnumerable<Thesis> theses = request.SemesterId.HasValue
            ? allTheses.Where(t =>
                t.SemesterId == request.SemesterId ||
                t.Team?.SemesterId == request.SemesterId)
            : allTheses;
        var checklists = await _checklistRepository.GetAllAsync();

        using var package = new ExcelPackage();
        var summaryWorksheet = package.Workbook.Worksheets.Add(SummarySheetName);
        var thesisInfoWorksheet = package.Workbook.Worksheets.Add(ThesisInfoSheetName);

        WriteSummaryHeaders(summaryWorksheet);
        WriteSummaryRows(summaryWorksheet, reviewerEmails);
        WriteThesisInfoSheet(thesisInfoWorksheet, theses);

        var thesisList = theses.ToList();
        foreach (var reviewer in reviewerList)
        {
            var sheetTitle = GetEmailPrefix(reviewer.Email) ?? reviewer.Email;
            var reviewerWorksheet = package.Workbook.Worksheets.Add(sheetTitle);
            WriteReviewerSheet(reviewerWorksheet, reviewer.Email, thesisList, checklists);
        }

        if (summaryWorksheet.Dimension != null)
        {
            summaryWorksheet.Cells[summaryWorksheet.Dimension.Address].AutoFitColumns();
        }

        if (thesisInfoWorksheet.Dimension != null)
        {
            thesisInfoWorksheet.Cells[thesisInfoWorksheet.Dimension.Address].AutoFitColumns();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    private static void WriteSummaryHeaders(ExcelWorksheet worksheet)
    {
        worksheet.Cells[1, 1].Value = "Thẩm định viên";
        worksheet.Cells[1, 2].Value = "";
        worksheet.Cells[1, 3].Value = "";
        worksheet.Cells[1, 4].Value = "Số lượng thẩm định ";

        ApplyHeaderStyle(worksheet.Cells[1, 1, 1, 4]);
    }

    private static void WriteSummaryRows(
        ExcelWorksheet worksheet,
        IEnumerable<string> reviewerEmails
    )
    {
        var reviewerKeys = NormalizeReviewerKeys(reviewerEmails);

        for (var index = 0; index < reviewerKeys.Count; index++)
        {
            var row = index + 2;
            worksheet.Cells[row, 1].Value = reviewerKeys[index];

            worksheet.Cells[row, 2].Formula =
                $"COUNTIFS('{ThesisInfoSheetName}'!S:S,'{SummarySheetName}'!A{row})";

            worksheet.Cells[row, 3].Formula =
                $"COUNTIFS('{ThesisInfoSheetName}'!$T:$T,'{SummarySheetName}'!A{row})";

            worksheet.Cells[row, 4].Formula = $"SUM(B{row}:C{row})";
        }
    }

    private static List<string> NormalizeReviewerKeys(IEnumerable<string> reviewerEmails)
    {
        var reviewerKeys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reviewerEmail in reviewerEmails ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(reviewerEmail))
            {
                continue;
            }

            var trimmedEmail = reviewerEmail.Trim();
            var atIndex = trimmedEmail.IndexOf('@');
            var reviewerKey = trimmedEmail;
            if (atIndex > 0)
            {
                reviewerKey = trimmedEmail[..atIndex];
            }

            reviewerKey = reviewerKey.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(reviewerKey) || !seen.Add(reviewerKey))
            {
                continue;
            }

            reviewerKeys.Add(reviewerKey);
        }

        return reviewerKeys;
    }

    private static void WriteThesisInfoSheet(ExcelWorksheet worksheet, IEnumerable<Thesis> theses)
    {
        var allTheses = theses.ToList();

        var maxRound = allTheses
            .SelectMany(t => t.ThesisReviewEvents)
            .Where(e => e.EventType == "FINAL_DECISION" && e.Round.HasValue)
            .Select(e => e.Round!.Value)
            .DefaultIfEmpty(0)
            .Max();

        WriteThesisInfoHeaders(worksheet, maxRound);

        var currentRow = 2;
        var globalStudentIndex = 0;

        foreach (var thesis in allTheses)
        {
            var team = thesis.Team;
            var members = team
                ?.Teammembers.Where(teamMember => teamMember.Student != null)
                .OrderBy(teamMember => teamMember.Student.StudentCode)
                .ThenBy(teamMember => teamMember.Student.Email)
                .ToList() ?? [];

            var startRow = currentRow;

            if (members.Count == 0)
            {
                currentRow++;
            }
            else
            {
                foreach (var member in members)
                {
                    var student = member.Student;
                    worksheet.Cells[currentRow, 1].Value = student.Email;
                    worksheet.Cells[currentRow, 3].Value = student.StudentCode;
                    worksheet.Cells[currentRow, 4].Value = student.FullName;
                    worksheet.Cells[currentRow, 5].Value = student.AccountDetail?.Major;

                    var qualification =
                        globalStudentIndex % 10 >= 7
                            ? "Không đủ điều kiện"
                            : "Đủ điều kiện";
                    worksheet.Cells[currentRow, 12].Value = qualification;
                    if (qualification == "Không đủ điều kiện")
                    {
                        worksheet.Cells[currentRow, 12].Style.Fill.PatternType =
                            ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, 12].Style.Fill.BackgroundColor.SetColor(
                            KhongDuDieuKienColor
                        );
                    }

                    currentRow++;
                    globalStudentIndex++;
                }
            }

            var endRow = currentRow - 1;

            SetMergedValue(worksheet, startRow, endRow, 2, team?.TeamCode);
            SetMergedValue(worksheet, startRow, endRow, 6, thesis.ThesisNameEn);
            SetMergedValue(worksheet, startRow, endRow, 7, thesis.ThesisNameVi);
            SetMergedValue(worksheet, startRow, endRow, 8, thesis.Abbreviation);
            SetMergedValue(worksheet, startRow, endRow, 9, thesis.Mentor1?.FullName);
            SetMergedValue(worksheet, startRow, endRow, 10, thesis.Mentor2?.FullName);
            SetMergedValue(worksheet, startRow, endRow, 11, thesis.ShortDescription);

            SetMergedValue(
                worksheet,
                startRow,
                endRow,
                13,
                thesis.IsFromEnterprise ? "x" : null
            );
            SetMergedValue(worksheet, startRow, endRow, 14, thesis.EnterpriseName);
            SetMergedValue(worksheet, startRow, endRow, 15, thesis.IsApplied ? "x" : null);
            SetMergedValue(worksheet, startRow, endRow, 16, thesis.IsAppUsed ? "x" : null);
            SetMergedValue(worksheet, startRow, endRow, 17, null);

            SetMergedValueWithBackground(
                worksheet,
                startRow,
                endRow,
                18,
                null,
                InfoEditColumnColor
            );

            var reviewerAssignments = thesis
                .ThesisReviewEvents.Where(e =>
                    e.EventType == "REVIEWER_ASSIGNED" && e.ActorRole == "REVIEWER"
                )
                .OrderBy(e => e.SequenceNo)
                .ThenBy(e => e.CreatedAt)
                .ToList();

            SetMergedValue(
                worksheet,
                startRow,
                endRow,
                19,
                GetEmailPrefix(reviewerAssignments.ElementAtOrDefault(0)?.ActorUser?.Email)
            );
            SetMergedValue(
                worksheet,
                startRow,
                endRow,
                20,
                GetEmailPrefix(reviewerAssignments.ElementAtOrDefault(1)?.ActorUser?.Email)
            );

            var finalDecisionsByRound = thesis
                .ThesisReviewEvents.Where(e => e.EventType == "FINAL_DECISION" && e.Round.HasValue)
                .GroupBy(e => e.Round!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.CreatedAt).ToList());

            for (var round = 1; round <= maxRound; round++)
            {
                var colBase = 21 + (round - 1) * 3;

                finalDecisionsByRound.TryGetValue(round, out var roundEvents);
                var firstEvent = roundEvents?.FirstOrDefault();

                SetMergedValue(
                    worksheet,
                    startRow,
                    endRow,
                    colBase,
                    MapDecision(firstEvent?.Decision)
                );

                SetMergedValue(
                    worksheet,
                    startRow,
                    endRow,
                    colBase + 1,
                    firstEvent?.CreatedAt.ToString("d-MMM")
                );

                var allComments = (roundEvents ?? [])
                    .SelectMany(e => e.Comments)
                    .Where(c => c.ParentCommentId == null)
                    .OrderBy(c => c.CreatedAt)
                    .ToList();

                var commentText =
                    allComments.Count > 0
                        ? string.Join(
                            "\n\n",
                            allComments.Select(c =>
                                $"{c.AuthorUser?.FullName ?? "Unknown"}: {c.Body}"
                            )
                        )
                        : null;

                SetMergedValue(worksheet, startRow, endRow, colBase + 2, commentText);
            }
        }

        var lastRow = Math.Max(currentRow - 1, 1);
        var lastColumn = maxRound > 0 ? 20 + maxRound * 3 : 20;
        var dataRange = worksheet.Cells[1, 1, lastRow, lastColumn];
        ApplyBorderStyle(dataRange);

        if (lastRow >= 1)
        {
            dataRange.AutoFilter = true;
        }

        worksheet.View.FreezePanes(2, 1);
    }

    private static void WriteReviewerSheet(
        ExcelWorksheet worksheet,
        string reviewerEmail,
        IReadOnlyList<Thesis> allTheses,
        IReadOnlyList<Checklist> checklists
    )
    {
        // Fixed headers A-I
        worksheet.Cells[1, 1].Value = "Ngày";
        worksheet.Cells[1, 2].Value = "Mentor 1";
        worksheet.Cells[1, 3].Value = "Mentor 2";
        worksheet.Cells[1, 4].Value = "Tên đề tài (EN)";
        worksheet.Cells[1, 5].Value = "Tền đề tài (VI)";
        worksheet.Cells[1, 6].Value = "Comment";
        worksheet.Cells[1, 7].Value = "Kết quả";
        worksheet.Cells[1, 8].Value = "Yes";
        worksheet.Cells[1, 9].Value = "No";

        // Checklist headers starting at J (column 10)
        var orderedChecklists = checklists.OrderBy(c => c.ChecklistId).ToList();
        for (var i = 0; i < orderedChecklists.Count; i++)
        {
            worksheet.Cells[1, 10 + i].Value = orderedChecklists[i].Content;
        }

        var lastHeaderCol = Math.Max(9, 9 + orderedChecklists.Count);
        ApplyReviewerSheetHeaderStyle(worksheet.Cells[1, 1, 1, lastHeaderCol]);

        // Theses assigned to this reviewer
        var assignedTheses = allTheses
            .Where(t =>
                t.ThesisReviewEvents.Any(e =>
                    (e.EventType == "REVIEWER_ASSIGNED" || e.EventType == "REVIEWER_DECISION")
                    && string.Equals(
                        e.ActorUser?.Email,
                        reviewerEmail,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .ToList();

        var firstChecklistCol = orderedChecklists.Count > 0
            ? ColumnLetter(10)
            : null;
        var lastChecklistCol = orderedChecklists.Count > 0
            ? ColumnLetter(9 + orderedChecklists.Count)
            : null;

        var row = 2;
        foreach (var thesis in assignedTheses)
        {
            // Latest FINAL_DECISION for this thesis
            var finalDecision = thesis
                .ThesisReviewEvents.Where(e => e.EventType == "FINAL_DECISION")
                .OrderByDescending(e => e.Round ?? 0)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            // This reviewer's latest REVIEWER_DECISION
            var reviewerDecision = thesis
                .ThesisReviewEvents.Where(e =>
                    e.EventType == "REVIEWER_DECISION"
                    && string.Equals(
                        e.ActorUser?.Email,
                        reviewerEmail,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderByDescending(e => e.Round ?? 0)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            // A: date
            worksheet.Cells[row, 1].Value = finalDecision?.CreatedAt.ToString("d-MMM");

            // B-C: mentors
            worksheet.Cells[row, 2].Value = thesis.Mentor1?.FullName;
            worksheet.Cells[row, 3].Value = thesis.Mentor2?.FullName;

            // D-E: thesis names
            worksheet.Cells[row, 4].Value = thesis.ThesisNameEn;
            worksheet.Cells[row, 5].Value = thesis.ThesisNameVi;

            // F: combined top-level comments from the final decision event
            if (finalDecision != null)
            {
                var topLevelComments = finalDecision.Comments
                    .Where(c => c.ParentCommentId == null && !c.IsDeleted)
                    .OrderBy(c => c.CreatedAt)
                    .ToList();
                worksheet.Cells[row, 6].Value = topLevelComments.Count > 0
                    ? string.Join(
                        "\n\n",
                        topLevelComments.Select(c =>
                            $"{c.AuthorUser?.FullName ?? "Unknown"}: {c.Body}"
                        )
                    )
                    : null;
                worksheet.Cells[row, 6].Style.WrapText = true;
            }

            // G: result
            worksheet.Cells[row, 7].Value = MapDecision(finalDecision?.Decision);

            // H: Yes formula — count "X" across checklist columns
            if (orderedChecklists.Count > 0)
            {
                worksheet.Cells[row, 8].Formula =
                    $"COUNTIFS({firstChecklistCol}{row}:{lastChecklistCol}{row},\"X\")";

                // I: No formula — count "o" across checklist columns
                worksheet.Cells[row, 9].Formula =
                    $"COUNTIFS({firstChecklistCol}{row}:{lastChecklistCol}{row},\"o\")";
            }

            // J+: checklist results from reviewer's REVIEWER_DECISION event
            var checklistResultMap = reviewerDecision?.ChecklistResults
                .ToDictionary(cr => cr.ChecklistId, cr => cr.IsChecked)
                ?? new Dictionary<int, bool>();

            for (var i = 0; i < orderedChecklists.Count; i++)
            {
                var checklist = orderedChecklists[i];
                var value = checklistResultMap.TryGetValue(checklist.ChecklistId, out var isChecked)
                    ? (isChecked ? "x" : "o")
                    : "o";
                worksheet.Cells[row, 10 + i].Value = value;
            }

            row++;
        }

        var lastDataRow = Math.Max(row - 1, 1);
        ApplyBorderStyle(worksheet.Cells[1, 1, lastDataRow, lastHeaderCol]);

        if (lastDataRow > 1)
        {
            worksheet.Cells[1, 1, lastDataRow, lastHeaderCol].AutoFilter = true;
        }

        worksheet.View.FreezePanes(2, 1);

        if (worksheet.Dimension != null)
        {
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }

    private static string ColumnLetter(int col)
    {
        var letter = string.Empty;
        while (col > 0)
        {
            col--;
            letter = (char)('A' + col % 26) + letter;
            col /= 26;
        }
        return letter;
    }

    private static string? MapDecision(string? decision) =>
        decision switch
        {
            "Pass" => "OK",
            "Fail" => "Consider",
            _ => null,
        };

    private static string? GetEmailPrefix(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        email = email.Trim();
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex].ToLowerInvariant() : email.ToLowerInvariant();
    }

    private static void WriteThesisInfoHeaders(ExcelWorksheet worksheet, int maxRound)
    {
        worksheet.Cells[1, 1].Value = "Student's Fpt Email";
        worksheet.Cells[1, 2].Value = "Nhóm";
        worksheet.Cells[1, 3].Value = "Roll Number";
        worksheet.Cells[1, 4].Value = "Full name";
        worksheet.Cells[1, 5].Value = "Khung";
        worksheet.Cells[1, 6].Value = "Project English Name";
        worksheet.Cells[1, 7].Value = "Project Vietnamese Name";
        worksheet.Cells[1, 8].Value = "Abbreviation";
        worksheet.Cells[1, 9].Value = "Supervisor";
        worksheet.Cells[1, 10].Value = "Supervisor 2";
        worksheet.Cells[1, 11].Value = "Description";
        worksheet.Cells[1, 12].Value = "Kết quả xét";
        worksheet.Cells[1, 13].Value = "Đề tài đến từ doanh nghiệp";
        worksheet.Cells[1, 14].Value = "Doanh nghiệp";
        worksheet.Cells[1, 15].Value = "Đề tài có tính ứng dụng";
        worksheet.Cells[1, 16].Value = "Đề tài có sử dụng app";
        worksheet.Cells[1, 17].Value = "Lịch hướng dẫn";

        ApplyHeaderStyle(worksheet.Cells[1, 1, 1, 17]);

        var rHeaderCell = worksheet.Cells[1, 18];
        rHeaderCell.Value = "Thông tin cần chỉnh sửa";
        rHeaderCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        rHeaderCell.Style.Fill.BackgroundColor.SetColor(InfoEditColumnColor);
        rHeaderCell.Style.Font.Bold = true;
        rHeaderCell.Style.Font.Size = 12;
        rHeaderCell.Style.Font.Color.SetColor(Color.Yellow);
        rHeaderCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        rHeaderCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ApplyBorderStyle(rHeaderCell);

        foreach (var col in new[] { 19, 20 })
        {
            var cell = worksheet.Cells[1, col];
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(ReviewerHeaderBackgroundColor);
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.Black);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBorderStyle(cell);
        }

        worksheet.Cells[1, 19].Value = "GV1";
        worksheet.Cells[1, 20].Value = "GV2";

        for (var round = 1; round <= maxRound; round++)
        {
            var colBase = 21 + (round - 1) * 3;
            worksheet.Cells[1, colBase].Value = $"Thẩm định lần {round}";
            worksheet.Cells[1, colBase + 1].Value = $"Thẩm định lần {round} date";
            worksheet.Cells[1, colBase + 2].Value = $"Thẩm định lần {round} comment";
            foreach (var col in new[] { colBase, colBase + 1, colBase + 2 })
            {
                var cell = worksheet.Cells[1, col];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(ReviewerHeaderBackgroundColor);
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.Black);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ApplyBorderStyle(cell);
            }
        }
    }

    private static void SetMergedValue(
        ExcelWorksheet worksheet,
        int startRow,
        int endRow,
        int column,
        string? value
    )
    {
        if (startRow < endRow)
        {
            for (var row = startRow + 1; row <= endRow; row++)
            {
                worksheet.Cells[row, column].Value = null;
            }
        }

        var range = worksheet.Cells[startRow, column, endRow, column];
        range.Merge = startRow != endRow;
        // Only populate the top-left cell so COUNTIFS counts each thesis once,
        // not once per student row in the merged block.
        worksheet.Cells[startRow, column].Value = value;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        range.Style.WrapText = true;
    }

    private static void SetMergedValueWithBackground(
        ExcelWorksheet worksheet,
        int startRow,
        int endRow,
        int column,
        string? value,
        Color backgroundColor
    )
    {
        if (startRow < endRow)
        {
            for (var row = startRow + 1; row <= endRow; row++)
            {
                worksheet.Cells[row, column].Value = null;
            }
        }

        var range = worksheet.Cells[startRow, column, endRow, column];
        range.Merge = startRow != endRow;
        worksheet.Cells[startRow, column].Value = value;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        range.Style.WrapText = true;
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(backgroundColor);
    }

    private static void ApplyReviewerSheetHeaderStyle(ExcelRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.Color.SetColor(Color.Black);
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ApplyBorderStyle(range);
    }

    private static void ApplyHeaderStyle(ExcelRange range)
    {
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(HeaderBackgroundColor);
        range.Style.Font.Bold = true;
        range.Style.Font.Color.SetColor(Color.White);
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ApplyBorderStyle(range);
    }

    private static void ApplyBorderStyle(ExcelRange range)
    {
        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
    }
}
