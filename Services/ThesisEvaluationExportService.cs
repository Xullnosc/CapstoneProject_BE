using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    private readonly ILecturerRepository _lecturerRepository;
    private readonly IThesisRepository _thesisRepository;

    public ThesisEvaluationExportService(
        ILecturerRepository lecturerRepository,
        IThesisRepository thesisRepository
    )
    {
        _lecturerRepository = lecturerRepository;
        _thesisRepository = thesisRepository;
    }

    public async Task<byte[]> GenerateWorkbookAsync(
        ReviewerSummarySheetRequestDTO request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviewers = await _lecturerRepository.GetReviewersAsync();
        var reviewerEmails = reviewers
            .Select(reviewer => reviewer.Email)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .ToList();
        var theses = await _thesisRepository.GetThesesForEvaluationExportAsync(request.SemesterId);

        using var package = new ExcelPackage();
        var summaryWorksheet = package.Workbook.Worksheets.Add(SummarySheetName);
        var thesisInfoWorksheet = package.Workbook.Worksheets.Add(ThesisInfoSheetName);

        WriteSummaryHeaders(summaryWorksheet);
        WriteSummaryRows(summaryWorksheet, reviewerEmails);
        WriteThesisInfoSheet(thesisInfoWorksheet, theses);

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
        worksheet.Cells[1, 2].Value = "Nothing";
        worksheet.Cells[1, 3].Value = "Nothing";
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

            // Count rows where reviewer 1 matches the reviewer key in column A.
            worksheet.Cells[row, 2].Formula =
                $"COUNTIFS('{ThesisInfoSheetName}'!S:S,'{SummarySheetName}'!A{row})";

            // Count rows where reviewer 2 matches the reviewer key in column A.
            worksheet.Cells[row, 3].Formula =
                $"COUNTIFS('{ThesisInfoSheetName}'!$T:$T,'{SummarySheetName}'!A{row})";

            // Total review load is the sum of reviewer 1 and reviewer 2 matches.
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
        WriteThesisInfoHeaders(worksheet);

        var currentRow = 2;
        foreach (var thesis in theses.Where(thesis => thesis.Team != null))
        {
            var team = thesis.Team!;
            var members = team
                .Teammembers.Where(teamMember => teamMember.Student != null)
                .OrderBy(teamMember => teamMember.Student.StudentCode)
                .ThenBy(teamMember => teamMember.Student.Email)
                .ToList();

            if (members.Count == 0)
            {
                continue;
            }

            var startRow = currentRow;

            foreach (var member in members)
            {
                var student = member.Student;
                worksheet.Cells[currentRow, 1].Value = student.Email;
                worksheet.Cells[currentRow, 3].Value = student.StudentCode;
                worksheet.Cells[currentRow, 4].Value = student.FullName;
                worksheet.Cells[currentRow, 5].Value = student.UserId;
                currentRow++;
            }

            var endRow = currentRow - 1;

            SetMergedValue(worksheet, startRow, endRow, 2, team.TeamCode);
            SetMergedValue(worksheet, startRow, endRow, 6, thesis.ThesisNameEn);
            SetMergedValue(worksheet, startRow, endRow, 7, thesis.ThesisNameVi);
            SetMergedValue(worksheet, startRow, endRow, 8, thesis.Abbreviation);
            SetMergedValue(worksheet, startRow, endRow, 9, thesis.Mentor1?.FullName);
            SetMergedValue(worksheet, startRow, endRow, 10, thesis.Mentor2?.FullName);
            SetMergedValue(worksheet, startRow, endRow, 11, thesis.ShortDescription);
        }

        var lastRow = Math.Max(currentRow - 1, 1);
        var lastColumn = 11;
        var dataRange = worksheet.Cells[1, 1, lastRow, lastColumn];
        ApplyBorderStyle(dataRange);

        if (lastRow >= 1)
        {
            dataRange.AutoFilter = true;
        }

        worksheet.View.FreezePanes(2, 1);
    }

    private static void WriteThesisInfoHeaders(ExcelWorksheet worksheet)
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

        ApplyHeaderStyle(worksheet.Cells[1, 1, 1, 11]);
    }

    private static void SetMergedValue(
        ExcelWorksheet worksheet,
        int startRow,
        int endRow,
        int column,
        string? value
    )
    {
        var range = worksheet.Cells[startRow, column, endRow, column];
        range.Merge = startRow != endRow;
        range.Value = value;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        range.Style.WrapText = true;
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
