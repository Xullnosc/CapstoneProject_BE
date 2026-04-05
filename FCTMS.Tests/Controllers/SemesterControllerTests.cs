using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using BusinessObjects;
using BusinessObjects.DTOs;
using CapstoneProject_BE.Controllers;

namespace FCTMS.Tests.Controllers
{
    public class SemesterControllerTests
    {
        private readonly Mock<ISemesterService> _mockSemesterService;
        private readonly Mock<IImportService> _mockImportService;
        private readonly SemesterController _controller;

        public SemesterControllerTests()
        {
            _mockSemesterService = new Mock<ISemesterService>();
            _mockImportService = new Mock<IImportService>();
            _controller = new SemesterController(_mockSemesterService.Object, _mockImportService.Object);

             // Mock User (ClaimsPrincipal) - Role HOD
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, CampusConstants.Roles.HOD)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        // --- Normal Cases (Happy Path) ---

        [Fact]
        public async Task GetSemesters_ReturnsOk()
        {
            // Arrange
            var semesters = new List<SemesterDTO> 
            { 
                new SemesterDTO { SemesterId = 1, SemesterName = "SP26" } 
            };
            
            _mockSemesterService.Setup(x => x.GetAllSemestersAsync())
                .ReturnsAsync(semesters);

            // Act
            var result = await _controller.GetSemesters();

            // Assert
            result.Value.Should().BeEquivalentTo(semesters);
        }

        [Fact]
        public async Task GetSemester_ExistingId_ReturnsOk()
        {
            // Arrange
            int id = 1;
            var semester = new SemesterDTO { SemesterId = id, SemesterName = "SP26" };

            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(id))
                .ReturnsAsync(semester);

            // Act
            var result = await _controller.GetSemester(id);

            // Assert
             result.Value.Should().BeEquivalentTo(semester);
        }

        [Fact]
        public async Task CreateSemester_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var request = new SemesterCreateDTO { SemesterName = "SP26"};
            var created = new SemesterDTO { SemesterId = 1, SemesterName = "SP26" };

            _mockSemesterService.Setup(x => x.CreateSemesterAsync(request))
                .ReturnsAsync(created);

            // Act
            var result = await _controller.CreateSemester(request);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task UpdateSemester_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            int id = 1;
            var request = new SemesterCreateDTO { SemesterId = id, SemesterName = "FA26" };

            _mockSemesterService.Setup(x => x.UpdateSemesterAsync(request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSemester(id, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }



        [Fact]
        public async Task EndSemester_ValidId_ReturnsOk()
        {
             // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value!.ToString().Should().Contain("ended successfully");
        }


        // --- Abnormal Cases (Abnormal & Edge Cases) ---

        [Fact]
        public async Task GetSemester_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(id))
                .ReturnsAsync((SemesterDTO)null!);

            // Act
            var result = await _controller.GetSemester(id);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateSemester_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            int id = 1;
            var request = new SemesterCreateDTO { SemesterId = 999 }; // Mismatch ID

            // Act
            var result = await _controller.UpdateSemester(id, request);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task EndSemester_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .ThrowsAsync(new KeyNotFoundException("Semester not found"));

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeEquivalentTo(new { message = "Semester not found" });
        }

        [Fact]
        public async Task EndSemester_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
                .ThrowsAsync(new Exception("Database corruption"));

            // Act
            var result = await _controller.EndSemester(id);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
            serverError.Value!.ToString().Should().Contain("An error occurred");
        }

        [Fact]
        public async Task CreateSemester_DuplicateCode_ReturnsBadRequest()
        {
            // Arrange
            var request = new SemesterCreateDTO { SemesterCode = "SP26" };
            _mockSemesterService.Setup(x => x.CreateSemesterAsync(request))
                .ThrowsAsync(new InvalidOperationException("Semester code 'SP26' already exists."));

            // Act
            var result = await _controller.CreateSemester(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value!.ToString().Should().Contain("Semester code 'SP26' already exists.");
        }

        [Fact]
        public async Task UpdateSemester_DuplicateCode_ReturnsBadRequest()
        {
            // Arrange
            int id = 1;
            var request = new SemesterCreateDTO { SemesterId = id, SemesterCode = "SP26" };
            _mockSemesterService.Setup(x => x.UpdateSemesterAsync(request))
                .ThrowsAsync(new InvalidOperationException("Semester code 'SP26' already exists."));

            // Act
            var result = await _controller.UpdateSemester(id, request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value!.ToString().Should().Contain("Semester code 'SP26' already exists.");
        }

        // --- Orphaned Students ---

        [Fact]
        public async Task GetOrphanedStudents_ValidSemester_ReturnsOk()
        {
            // Arrange
            int id = 1;
            var orphaned = new List<WhitelistDTO>
            {
                new WhitelistDTO { WhitelistId = 1, Email = "orphan@fpt.edu.vn", FullName = "Orphan Student" }
            };

            _mockSemesterService.Setup(x => x.GetOrphanedStudentsAsync(id, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedResult<WhitelistDTO>(orphaned, orphaned.Count, 1, 10));

            // Act
            var result = await _controller.GetOrphanedStudents(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var actual = okResult.Value as PagedResult<WhitelistDTO>;
            actual.Should().NotBeNull();
            actual!.Items.Should().BeEquivalentTo(orphaned);
        }

        [Fact]
        public async Task GetOrphanedStudents_SemesterNotFound_ReturnsNotFound()
        {
            // Arrange
            int id = 99;
            _mockSemesterService.Setup(x => x.GetOrphanedStudentsAsync(id, It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException("Semester 99 not found"));

            // Act
            var result = await _controller.GetOrphanedStudents(id);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeEquivalentTo(new { message = "Semester 99 not found" });
        }

        [Fact]
        public async Task GetOrphanedStudents_ServiceThrows_Returns500()
        {
            // Arrange
            int id = 1;
            _mockSemesterService.Setup(x => x.GetOrphanedStudentsAsync(id, It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act
            var result = await _controller.GetOrphanedStudents(id);

            // Assert
            var serverError = result.Should().BeOfType<ObjectResult>().Subject;
            serverError.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateSemester_ReturnsCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new SemesterCreateDTO { SemesterCode = "SP27", SemesterName = "Spring 2027" };
            var created = new SemesterDTO { SemesterId = 10, SemesterCode = "SP27", SemesterName = "Spring 2027" };
            _mockSemesterService.Setup(x => x.CreateSemesterAsync(dto)).ReturnsAsync(created);

            // Act
            var result = await _controller.CreateSemester(dto);

            // Assert — controller returns CreatedAtAction (201)
            result.Result.Should().BeOfType<CreatedAtActionResult>()
                .Which.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task CreateSemester_ReturnsBadRequest_WhenCodeAlreadyExists()
        {
            // Arrange
            var dto = new SemesterCreateDTO { SemesterCode = "SP26" };
            _mockSemesterService.Setup(x => x.CreateSemesterAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Semester code 'SP26' already exists."));

            // Act
            var result = await _controller.CreateSemester(dto);

            // Assert
            var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value!.ToString().Should().Contain("SP26");
        }

        [Fact]
        public async Task CreateSemester_ReturnsBadRequest_WhenDatesOverlap()
        {
            // Arrange
            var dto = new SemesterCreateDTO { SemesterCode = "XX01" };
            _mockSemesterService.Setup(x => x.CreateSemesterAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Semester dates overlap"));

            // Act
            var result = await _controller.CreateSemester(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSemester_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var dto = new SemesterCreateDTO { SemesterId = 1, SemesterCode = "SU27" };
            _mockSemesterService.Setup(x => x.UpdateSemesterAsync(dto)).Returns(Task.CompletedTask);

            // Act — controller signature: UpdateSemester(int id, SemesterCreateDTO dto)
            var result = await _controller.UpdateSemester(1, dto);

            // Assert — returns 204 NoContent on success
            result.Should().BeOfType<NoContentResult>();
            _mockSemesterService.Verify(x => x.UpdateSemesterAsync(dto), Times.Once);
        }

        [Fact]
        public async Task UpdateSemester_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange — id in route != id in body
            var dto = new SemesterCreateDTO { SemesterId = 5 };

            // Act
            var result = await _controller.UpdateSemester(999, dto);

            // Assert — controller returns 400 when ids don't match
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task UpdateSemester_ReturnsBadRequest_WhenDatesOverlap()
        {
            // Arrange
            var dto = new SemesterCreateDTO { SemesterId = 2, SemesterCode = "XX" };
            _mockSemesterService.Setup(x => x.UpdateSemesterAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Semester dates overlap"));

            // Act
            var result = await _controller.UpdateSemester(2, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // [Fact]
        // public async Task EndSemester_ReturnsOk_WhenSuccessful()
        // {
        //     // Arrange
        //     int id = 1;
        //     _mockSemesterService.Setup(x => x.EndSemesterAsync(id)).Returns(Task.CompletedTask);
        // 
        //     // Act
        //     var result = await _controller.EndSemester(id);
        // 
        //     // Assert
        //     result.Should().BeOfType<OkObjectResult>();
        //     _mockSemesterService.Verify(x => x.EndSemesterAsync(id), Times.Once);
        // }
        // 
        // [Fact]
        // public async Task EndSemester_ReturnsNotFound_WhenIdDoesNotExist()
        // {
        //     // Arrange
        //     int id = 999;
        //     _mockSemesterService.Setup(x => x.EndSemesterAsync(id))
        //         .ThrowsAsync(new KeyNotFoundException($"Semester with ID {id} not found."));
        // 
        //     // Act
        //     var result = await _controller.EndSemester(id);
        // 
        //     // Assert
        //     result.Should().BeOfType<NotFoundObjectResult>();
        // }

        [Fact]
        public async Task GetSemester_ReturnsOk_WhenFound()
        {
            // Arrange
            int id = 1;
            var semester = new SemesterDTO { SemesterId = id, SemesterCode = "SP26" };
            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(id)).ReturnsAsync(semester);

            // Act — action is named GetSemester(int id)
            var result = await _controller.GetSemester(id);

            // Assert — returns 200 with the semester DTO implicitly wrapped
            result.Value.Should().BeEquivalentTo(semester);
        }

        [Fact]
        public async Task GetSemester_ReturnsNotFound_WhenNull()
        {
            // Arrange
            _mockSemesterService.Setup(x => x.GetSemesterByIdAsync(404)).ReturnsAsync((SemesterDTO?)null);

            // Act
            var result = await _controller.GetSemester(404);

            // Assert — controller returns NotFound when service returns null
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetOrphanedStudents_ReturnsOk_WithEmptyList_WhenNoOrphans()
        {
            // Arrange
            int id = 1;
            var empty = new PagedResult<WhitelistDTO>(new List<WhitelistDTO>(), 0, 1, 10);
            _mockSemesterService.Setup(x => x.GetOrphanedStudentsAsync(id, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(empty);

            // Act
            var result = await _controller.GetOrphanedStudents(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }
    }
}
