using System.IO;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using OfficeOpenXml;

namespace Services.Helpers
{
    public class ImportHelper : IImportHelper
    {
        private static readonly Dictionary<int, string> RoleNameMapping = new()
        {
            { 1, "HOD" },
            { 2, "Lecturer" },
            { 3, "Student" },
            { 4, "Admin" }
        };

        public ImportResult<WhitelistImportDTO> ImportWhitelistFromExcel(Stream excelStream)
        {
            if (excelStream == null || excelStream.Length == 0)
            {
                throw new ArgumentException("Excel stream is null or empty");
            }

            // EPPlus license context is set globally in Program.cs

            excelStream.Position = 0; // Reset stream position
            var result = new ImportResult<WhitelistImportDTO>();

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
            {
                throw new ArgumentException("Excel worksheet is empty");
            }

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerAliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeHeaderKey(CampusConstants.WhitelistImportColumns.Email)] = CampusConstants.WhitelistImportColumns.Email,
                [NormalizeHeaderKey("E-mail")] = CampusConstants.WhitelistImportColumns.Email,
                [NormalizeHeaderKey(CampusConstants.WhitelistImportColumns.StudentCode)] = CampusConstants.WhitelistImportColumns.StudentCode,
                [NormalizeHeaderKey("Student Code")] = CampusConstants.WhitelistImportColumns.StudentCode,
                [NormalizeHeaderKey(CampusConstants.WhitelistImportColumns.FullName)] = CampusConstants.WhitelistImportColumns.FullName,
                [NormalizeHeaderKey("Full Name")] = CampusConstants.WhitelistImportColumns.FullName,
                [NormalizeHeaderKey(CampusConstants.WhitelistImportColumns.SemesterCode)] = CampusConstants.WhitelistImportColumns.SemesterCode,
                [NormalizeHeaderKey("Semester Code")] = CampusConstants.WhitelistImportColumns.SemesterCode,
                [NormalizeHeaderKey("Semester")] = CampusConstants.WhitelistImportColumns.SemesterCode,
                [NormalizeHeaderKey(CampusConstants.WhitelistImportColumns.Campus)] = CampusConstants.WhitelistImportColumns.Campus,
            };

            const int headerRow = 3;
            const int dataStartRow = 4;

            for (int col = 2; col <= worksheet.Dimension.End.Column; col++)
            {
                var rawHeader = worksheet.Cells[headerRow, col].Text;
                if (string.IsNullOrWhiteSpace(rawHeader))
                {
                    rawHeader = worksheet.Cells[headerRow, col].Value?.ToString();
                }

                var normalizedHeaderKey = NormalizeHeaderKey(rawHeader);
                if (string.IsNullOrWhiteSpace(normalizedHeaderKey))
                {
                    continue;
                }

                if (headerAliasMap.TryGetValue(normalizedHeaderKey, out var canonicalHeader) && !headerMap.ContainsKey(canonicalHeader))
                {
                    headerMap[canonicalHeader] = col;
                }
            }

            string[] requiredHeaders = new string[]
            {
                CampusConstants.WhitelistImportColumns.Email,
                CampusConstants.WhitelistImportColumns.StudentCode,
                CampusConstants.WhitelistImportColumns.FullName,
                CampusConstants.WhitelistImportColumns.SemesterCode,
            };

            foreach (var col in requiredHeaders)
            {
                if (!headerMap.ContainsKey(col))
                {
                    throw new ArgumentException($"Missing required column: {col}");
                }
            }

            for (int row = dataStartRow; row <= worksheet.Dimension.End.Row; row++)
            {
                try
                {
                    var email = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.Email]]
                        .Text.Trim();
                    if (string.IsNullOrEmpty(email))
                    {
                        // skip empty email rows but record as info
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.Email, Message = "Empty email, row skipped" });
                        continue;
                    }

                    // validate email format
                    try
                    {
                        _ = new MailAddress(email);
                    }
                    catch
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.Email, Message = "Invalid email format" });
                        continue;
                    }

                    var studentCodeRaw = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.StudentCode]]
                        .Text.Trim();
                    string? studentCode = string.IsNullOrEmpty(studentCodeRaw) ? null : studentCodeRaw;

                    if (string.IsNullOrWhiteSpace(studentCode))
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.StudentCode, Message = "StudentCode cannot be empty" });
                        continue;
                    }

                    var semesterCode = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.SemesterCode]]
                        .Text.Trim();

                    if (string.IsNullOrWhiteSpace(semesterCode))
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.SemesterCode, Message = "SemesterCode cannot be empty" });
                        continue;
                    }

                    var whitelistDto = new WhitelistImportDTO
                    {
                        RowNumber = row,
                        Email = email,
                        StudentCode = studentCode,
                        FullName = worksheet
                            .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.FullName]]
                            .Text.Trim(),
                        RoleId = 3,  // Always set to Student role
                        Role = "Student",
                        SemesterCode = semesterCode,
                    };

                    // Validate FullName is not empty
                    if (string.IsNullOrWhiteSpace(whitelistDto.FullName))
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.FullName, Message = "FullName cannot be empty" });
                        continue;
                    }

                    result.Items.Add(whitelistDto);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError { Row = row, Column = string.Empty, Message = ex.Message });
                }
            }

            return result;
        }

        private static string NormalizeHeaderKey(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return string.Empty;
            }

            var normalized = header.Normalize(NormalizationForm.FormKC).Trim();
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            return sb.ToString();
        }
    }
}
