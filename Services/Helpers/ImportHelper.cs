using System.IO;
using System.Globalization;
using System.Net.Mail;
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

            const int headerRow = 3;
            const int dataStartRow = 4;

            for (int col = 2; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[headerRow, col].Text.Trim();
                if (!string.IsNullOrEmpty(header) && !headerMap.ContainsKey(header))
                {
                    headerMap[header] = col;
                }
            }

            string[] requiredHeaders = new string[]
            {
                CampusConstants.WhitelistImportColumns.Email,
                CampusConstants.WhitelistImportColumns.StudentCode,
                CampusConstants.WhitelistImportColumns.FullName,
                CampusConstants.WhitelistImportColumns.RoleId,
                CampusConstants.WhitelistImportColumns.Campus,
                CampusConstants.WhitelistImportColumns.SemesterId,
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

                    var roleText = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.RoleId]]
                        .Text.Trim();
                    if (!int.TryParse(roleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int roleIdParsed) || roleIdParsed <= 0)
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.RoleId, Message = "Invalid RoleId" });
                        continue;
                    }

                    var semesterText = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.SemesterId]]
                        .Text.Trim();
                    if (!int.TryParse(semesterText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int semesterIdParsed) || semesterIdParsed <= 0)
                    {
                        result.Errors.Add(new ImportError { Row = row, Column = CampusConstants.WhitelistImportColumns.SemesterId, Message = "Invalid SemesterId" });
                        continue;
                    }

                    var whitelistDto = new WhitelistImportDTO
                    {
                        Email = email,
                        StudentCode = studentCode,
                        FullName = worksheet
                            .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.FullName]]
                            .Text.Trim(),
                        RoleId = roleIdParsed,
                        Role = RoleNameMapping.TryGetValue(roleIdParsed, out var rName) ? rName : "Unknown",
                        Campus = worksheet
                            .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.Campus]]
                            .Text.Trim(),
                        SemesterId = semesterIdParsed,
                    };

                    result.Items.Add(whitelistDto);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError { Row = row, Column = string.Empty, Message = ex.Message });
                }
            }

            return result;
        }
    }
}
