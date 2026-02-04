using System.IO;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using OfficeOpenXml;

namespace Services.Helpers
{
    public class ImportHelper : IImportHelper
    {
        public List<WhitelistImportDTO> ImportWhitelistFromExcel(Stream excelStream)
        {
            if (excelStream == null || excelStream.Length == 0)
            {
                throw new ArgumentException("Excel stream is null or empty");
            }
            var importedWhiteLists = new List<WhitelistDTO>();

            excelStream.Position = 0; // Reset stream position
            var result = new List<WhitelistImportDTO>();
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
            {
                throw new ArgumentException("Excel worksheet is empty");
            }

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int col = 2; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[3, col].Text.Trim();
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

            for (int row = 4; row <= worksheet.Dimension.End.Row; row++)
            {
                var email = worksheet
                    .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.Email]]
                    .Text.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    continue; // Skip rows with empty email
                }
                var studentCode = worksheet
                    .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.StudentCode]]
                    .Text.Trim();
                if (string.IsNullOrEmpty(studentCode))
                {
                    studentCode = null; // Set to null if empty
                }
                if (
                    !int.TryParse(
                        worksheet
                            .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.RoleId]]
                            .Text.Trim(),
                        out int roleIdParsed
                    )
                    || roleIdParsed <= 0
                )
                {
                    throw new ArgumentException($"Invalid RoleId in row {row}");
                }

                if (
                    !int.TryParse(
                        worksheet
                            .Cells[
                                row,
                                headerMap[CampusConstants.WhitelistImportColumns.SemesterId]
                            ]
                            .Text.Trim(),
                        out int semesterIdParsed
                    )
                    || semesterIdParsed <= 0
                )
                {
                    throw new ArgumentException($"Invalid SemesterId in row {row}");
                }

                var whitelistDto = new WhitelistImportDTO
                {
                    Email = email,
                    StudentCode = studentCode,
                    FullName = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.FullName]]
                        .Text.Trim(),
                    RoleId = roleIdParsed,
                    Campus = worksheet
                        .Cells[row, headerMap[CampusConstants.WhitelistImportColumns.Campus]]
                        .Text.Trim(),
                    SemesterId = semesterIdParsed,
                };

                result.Add(whitelistDto);
            }

            return result;
        }
    }
}
