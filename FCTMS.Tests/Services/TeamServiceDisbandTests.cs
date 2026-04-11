using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using BusinessObjects.Models;
using BusinessObjects;
using Repositories;
using Services;
using Services.Helpers;
using BusinessObjects.Interfaces;

namespace FCTMS.Tests.Services
{
    public class TeamServiceDisbandTests
    {
        private readonly Mock<ITeamRepository> _mockTeamRepository = new();
        private readonly Mock<ISemesterRepository> _mockSemesterRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<ICloudinaryHelper> _mockCloudinaryHelper = new();
        private readonly Mock<ITeamMemberRepository> _mockTeamMemberRepository = new();
        private readonly Mock<IThesisRepository> _mockThesisRepository = new();
        private readonly Mock<ISemesterService> _mockSemesterService = new();
        private readonly Mock<IWhitelistRepository> _mockWhitelistRepository = new();
        private readonly Mock<ICampusContextService> _mockCampusContextService = new();
        private readonly Mock<INotificationService> _mockNotificationService = new();

        private TeamService CreateService()
        {
            return new TeamService(
                _mockTeamRepository.Object,
                _mockSemesterRepository.Object,
                _mockUserRepository.Object,
                _mockCloudinaryHelper.Object,
                _mockTeamMemberRepository.Object,
                _mockThesisRepository.Object,
                _mockSemesterService.Object,
                _mockWhitelistRepository.Object,
                _mockCampusContextService.Object,
                _mockNotificationService.Object
            );
        }

        private void SetupSuccessDisband(int teamId, int leaderId)
        {
            var team = new Team
            {
                TeamId = teamId,
                LeaderId = leaderId,
                SemesterId = 1,
                Status = "Active",
                Teammembers = new List<Teammember>()
            };

            _mockTeamRepository.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            _mockSemesterRepository.Setup(r => r.GetCurrentSemesterAsync())
                .ReturnsAsync(new Semester { SemesterId = 1, Status = "Open" });
            _mockTeamMemberRepository.Setup(r => r.RemoveAllMembersFromTeamAsync(teamId))
                .ReturnsAsync(true);
            _mockTeamRepository.Setup(r => r.UpdateAsync(It.IsAny<Team>()))
                .ReturnsAsync(true);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithDraftThesis_ShouldCancelThesis_Case1()
        {
            var service = CreateService();
            int teamId = 101;
            int leaderId = 201;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "501", TeamId = teamId, UserId = leaderId, Status = "Draft" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithDraftThesis_ShouldCancelThesis_Case2()
        {
            var service = CreateService();
            int teamId = 102;
            int leaderId = 202;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "502", TeamId = teamId, UserId = leaderId, Status = "Draft" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithDraftThesis_ShouldCancelThesis_Case3()
        {
            var service = CreateService();
            int teamId = 103;
            int leaderId = 203;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "503", TeamId = teamId, UserId = leaderId, Status = "Draft" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithOnMentorInvitingThesis_ShouldCancelThesis_Case1()
        {
            var service = CreateService();
            int teamId = 104;
            int leaderId = 204;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "504", TeamId = teamId, UserId = leaderId, Status = "On Mentor Inviting" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithOnMentorInvitingThesis_ShouldCancelThesis_Case2()
        {
            var service = CreateService();
            int teamId = 105;
            int leaderId = 205;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "505", TeamId = teamId, UserId = leaderId, Status = "On Mentor Inviting" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithOnMentorInvitingThesis_ShouldCancelThesis_Case3()
        {
            var service = CreateService();
            int teamId = 106;
            int leaderId = 206;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "506", TeamId = teamId, UserId = leaderId, Status = "On Mentor Inviting" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithReviewingThesis_ShouldCancelThesis_Case1()
        {
            var service = CreateService();
            int teamId = 107;
            int leaderId = 207;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "507", TeamId = teamId, UserId = leaderId, Status = "Reviewing" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithReviewingThesis_ShouldCancelThesis_Case2()
        {
            var service = CreateService();
            int teamId = 108;
            int leaderId = 208;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "508", TeamId = teamId, UserId = leaderId, Status = "Reviewing" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithReviewingThesis_ShouldCancelThesis_Case3()
        {
            var service = CreateService();
            int teamId = 109;
            int leaderId = 209;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "509", TeamId = teamId, UserId = leaderId, Status = "Reviewing" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithNeedUpdateThesis_ShouldCancelThesis_Case1()
        {
            var service = CreateService();
            int teamId = 110;
            int leaderId = 210;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "510", TeamId = teamId, UserId = leaderId, Status = "Need Update" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithNeedUpdateThesis_ShouldCancelThesis_Case2()
        {
            var service = CreateService();
            int teamId = 111;
            int leaderId = 211;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "511", TeamId = teamId, UserId = leaderId, Status = "Need Update" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }

        [Fact]
        public async Task DisbandTeamAsync_WithNeedUpdateThesis_ShouldCancelThesis_Case3()
        {
            var service = CreateService();
            int teamId = 112;
            int leaderId = 212;
            SetupSuccessDisband(teamId, leaderId);
            var pendingThesis = new Thesis { ThesisId = "512", TeamId = teamId, UserId = leaderId, Status = "Need Update" };
            _mockThesisRepository.Setup(r => r.GetThesesByUserIdAsync(leaderId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.GetThesesByTeamIdAsync(teamId)).ReturnsAsync(new List<Thesis> { pendingThesis });
            _mockThesisRepository.Setup(r => r.UpdateThesisAsync(It.IsAny<Thesis>())).Callback<Thesis>(t => { Assert.Equal("Cancelled", t.Status); }).Returns(Task.CompletedTask);
            var result = await service.DisbandTeamAsync(teamId, leaderId);
            Assert.True(result);
        }
    }
}
