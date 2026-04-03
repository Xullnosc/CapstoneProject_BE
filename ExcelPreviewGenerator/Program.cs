using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using Repositories;
using Services;

ExcelPackage.License.SetNonCommercialOrganization("Capstone Project");

var configuration = new ConfigurationBuilder()
    .SetBasePath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CapstoneProject_BE")
    )
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var connectionString =
    configuration.GetConnectionString("capstoneDb")
    ?? throw new InvalidOperationException("Connection string 'capstoneDb' was not found.");

var dbContextOptions = new DbContextOptionsBuilder<FctmsContext>()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
    .Options;

await using var dbContext = new FctmsContext(dbContextOptions);
var lecturerDao = new LecturerDAO(dbContext);
var lecturerRepository = new LecturerRepository(lecturerDao);
var thesisDao = new ThesisDAO(dbContext);
var thesisRepository = new ThesisRepository(thesisDao);
var exportService = new ThesisEvaluationExportService(lecturerRepository, thesisRepository);
var workbookBytes = await exportService.GenerateWorkbookAsync(new ReviewerSummarySheetRequestDTO());

const string outputDirectory = @"D:\ExcelTests";

Directory.CreateDirectory(outputDirectory);

var outputFileName = $"BangTongHopPreview_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
var outputPath = Path.Combine(outputDirectory, outputFileName);
await File.WriteAllBytesAsync(outputPath, workbookBytes);

Console.WriteLine($"Workbook generated: {outputPath}");
