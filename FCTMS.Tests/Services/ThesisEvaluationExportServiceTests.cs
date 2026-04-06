using BusinessObjects.DTOs;
using BusinessObjects.Models;
using FluentAssertions;
using Moq;
using OfficeOpenXml;
using Repositories;
using Services;

namespace FCTMS.Tests.Services;

public class ThesisEvaluationExportServiceTests
{
    private readonly Mock<ILecturerRepository> _lecturerRepository = new();
    private readonly Mock<IThesisRepository> _thesisRepository = new();
    private readonly Mock<IChecklistRepository> _checklistRepository = new();

    [Fact]
    public async Task GenerateWorkbookAsync_WritesSummarySheetHeadersAndFormulas()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");
        _lecturerRepository
            .Setup(repository => repository.GetReviewersAsync())
            .ReturnsAsync([
                new Lecturer { Email = "longnx6@fpt.com", IsReviewer = true },
                new Lecturer { Email = "linhpt@fpt.com", IsReviewer = true },
            ]);

        _thesisRepository
            .Setup(repository => repository.GetThesesForEvaluationExportAsync())
            .ReturnsAsync([]);

        _checklistRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        var service = new ThesisEvaluationExportService(
            _lecturerRepository.Object,
            _thesisRepository.Object,
            _checklistRepository.Object
        );

        var workbookBytes = await service.GenerateWorkbookAsync(
            new ReviewerSummarySheetRequestDTO()
        );

        using var stream = new MemoryStream(workbookBytes);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets["Bảng tổng hợp"];

        worksheet.Should().NotBeNull();
        worksheet!.Cells[1, 1].Text.Should().Be("Thẩm định viên");
        worksheet.Cells[1, 2].Text.Should().Be("Nothing");
        worksheet.Cells[1, 3].Text.Should().Be("Nothing");
        worksheet.Cells[1, 4].Text.Should().Be("Số lượng thẩm định ");

        worksheet.Cells[2, 1].Text.Should().Be("longnx6");
        worksheet
            .Cells[2, 2]
            .Formula.Should()
            .Be("COUNTIFS('Thông tin đề tài '!S:S,'Bảng tổng hợp'!A2)");
        worksheet
            .Cells[2, 3]
            .Formula.Should()
            .Be("COUNTIFS('Thông tin đề tài '!$T:$T,'Bảng tổng hợp'!A2)");
        worksheet.Cells[2, 4].Formula.Should().Be("SUM(B2:C2)");

        worksheet.Cells[3, 1].Text.Should().Be("linhpt");
        worksheet
            .Cells[3, 2]
            .Formula.Should()
            .Be("COUNTIFS('Thông tin đề tài '!S:S,'Bảng tổng hợp'!A3)");
        worksheet
            .Cells[3, 3]
            .Formula.Should()
            .Be("COUNTIFS('Thông tin đề tài '!$T:$T,'Bảng tổng hợp'!A3)");
        worksheet.Cells[3, 4].Formula.Should().Be("SUM(B3:C3)");
    }

    [Fact]
    public async Task GenerateWorkbookAsync_DeduplicatesReviewerKeysAndCreatesFormulaDependencySheet()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");
        _lecturerRepository
            .Setup(repository => repository.GetReviewersAsync())
            .ReturnsAsync([
                new Lecturer { Email = "LONGNX6@fpt.com", IsReviewer = true },
                new Lecturer { Email = "longnx6@fpt.com", IsReviewer = true },
                new Lecturer { Email = " reviewer2@fpt.com ", IsReviewer = true },
                new Lecturer { Email = string.Empty, IsReviewer = true },
            ]);

        _thesisRepository
            .Setup(repository => repository.GetThesesForEvaluationExportAsync())
            .ReturnsAsync([]);

        _checklistRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        var service = new ThesisEvaluationExportService(
            _lecturerRepository.Object,
            _thesisRepository.Object,
            _checklistRepository.Object
        );

        var workbookBytes = await service.GenerateWorkbookAsync(
            new ReviewerSummarySheetRequestDTO()
        );

        using var stream = new MemoryStream(workbookBytes);
        using var package = new ExcelPackage(stream);
        var summaryWorksheet = package.Workbook.Worksheets["Bảng tổng hợp"];
        var thesisInfoWorksheet = package.Workbook.Worksheets["Thông tin đề tài "];

        summaryWorksheet.Should().NotBeNull();
        thesisInfoWorksheet.Should().NotBeNull();
        summaryWorksheet!.Dimension!.End.Row.Should().Be(3);
        summaryWorksheet.Cells[2, 1].Text.Should().Be("longnx6");
        summaryWorksheet.Cells[3, 1].Text.Should().Be("reviewer2");
        _lecturerRepository.Verify(repository => repository.GetReviewersAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateWorkbookAsync_WritesThesisInfoSheetWithMergedTeamColumnsAndStyles()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");
        _lecturerRepository.Setup(repository => repository.GetReviewersAsync()).ReturnsAsync([]);
        _thesisRepository
            .Setup(repository => repository.GetThesesForEvaluationExportAsync())
            .ReturnsAsync([
                new Thesis
                {
                    ThesisId = "TH001",
                    ThesisNameEn = "Smart Campus Scheduler",
                    ThesisNameVi = "He thong lich thong minh",
                    Abbreviation = "SCS",
                    ShortDescription = "Scheduling system for campus operations",
                    Mentor1 = new Lecturer { FullName = "Mentor One" },
                    Mentor2 = new Lecturer { FullName = "Mentor Two" },
                    Team = new Team
                    {
                        TeamId = 10,
                        TeamCode = "TEAM-01",
                        TeamName = "Team 01",
                        Teammembers =
                        [
                            new Teammember
                            {
                                Student = new User
                                {
                                    UserId = 101,
                                    Email = "s1@fpt.edu.vn",
                                    StudentCode = "SE1001",
                                    FullName = "Student One",
                                    AccountDetail = new AccountDetail { Major = "Software Engineering" },
                                },
                            },
                            new Teammember
                            {
                                Student = new User
                                {
                                    UserId = 102,
                                    Email = "s2@fpt.edu.vn",
                                    StudentCode = "SE1002",
                                    FullName = "Student Two",
                                    AccountDetail = new AccountDetail { Major = "Artificial Intelligence" },
                                },
                            },
                        ],
                    },
                },
            ]);

        _checklistRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        var service = new ThesisEvaluationExportService(
            _lecturerRepository.Object,
            _thesisRepository.Object,
            _checklistRepository.Object
        );

        var workbookBytes = await service.GenerateWorkbookAsync(
            new ReviewerSummarySheetRequestDTO()
        );

        using var stream = new MemoryStream(workbookBytes);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets["Thông tin đề tài "];

        worksheet.Should().NotBeNull();
        worksheet!.Cells[1, 1].Text.Should().Be("Student's Fpt Email");
        worksheet.Cells[1, 2].Text.Should().Be("Nhóm");
        worksheet.Cells[1, 6].Text.Should().Be("Project English Name");
        worksheet.Cells[1, 11].Text.Should().Be("Description");

        worksheet.Cells[2, 1].Text.Should().Be("s1@fpt.edu.vn");
        worksheet.Cells[3, 1].Text.Should().Be("s2@fpt.edu.vn");
        worksheet.Cells[2, 3].Text.Should().Be("SE1001");
        worksheet.Cells[3, 3].Text.Should().Be("SE1002");
        worksheet.Cells[2, 4].Text.Should().Be("Student One");
        worksheet.Cells[3, 4].Text.Should().Be("Student Two");
        worksheet.Cells[2, 5].Text.Should().Be("Software Engineering");
        worksheet.Cells[3, 5].Text.Should().Be("Artificial Intelligence");

        worksheet.MergedCells.Should().Contain("B2:B3");
        worksheet.MergedCells.Should().Contain("F2:F3");
        worksheet.MergedCells.Should().Contain("G2:G3");
        worksheet.MergedCells.Should().Contain("H2:H3");
        worksheet.MergedCells.Should().Contain("I2:I3");
        worksheet.MergedCells.Should().Contain("J2:J3");
        worksheet.MergedCells.Should().Contain("K2:K3");

        worksheet.Cells[2, 2].Text.Should().Be("TEAM-01");
        worksheet.Cells[2, 6].Text.Should().Be("Smart Campus Scheduler");
        worksheet.Cells[2, 7].Text.Should().Be("He thong lich thong minh");
        worksheet.Cells[2, 8].Text.Should().Be("SCS");
        worksheet.Cells[2, 9].Text.Should().Be("Mentor One");
        worksheet.Cells[2, 10].Text.Should().Be("Mentor Two");
        worksheet.Cells[2, 11].Text.Should().Be("Scheduling system for campus operations");

        worksheet.Cells[1, 1, 3, 11].AutoFilter.Should().BeTrue();
        worksheet.Cells[1, 1].Style.Fill.BackgroundColor.Rgb.Should().Be("FF70AD47");
        worksheet
            .Cells[2, 2]
            .Style.HorizontalAlignment.Should()
            .Be(OfficeOpenXml.Style.ExcelHorizontalAlignment.Center);
    }
}
