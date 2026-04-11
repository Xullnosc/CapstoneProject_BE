using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using BusinessObjects.Models;
using Repositories;
using Services;
using Services.Helpers;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FCTMS.Tests.Services
{
    public class ThesisServiceSearchEnterprisesTests
    {
        private ThesisService CreateServiceWithContext(FctmsContext context)
        {
            return new ThesisService(
                new Mock<IThesisRepository>().Object,
                new Mock<IThesisReviewRepository>().Object,
                new Mock<ITeamRepository>().Object,
                new Mock<IUserRepository>().Object,
                new Mock<ICloudinaryHelper>().Object,
                new Mock<ISemesterRepository>().Object,
                new Mock<ILecturerRepository>().Object,
                new Mock<ITeamInvitationRepository>().Object,
                new Mock<ITeamMemberRepository>().Object,
                new Mock<IWhitelistRepository>().Object,
                new Mock<INotificationService>().Object,
                context,
                new Mock<IMapper>().Object,
                new Mock<ISystemParameterService>().Object,
                null, null, null, null,
                new Mock<ILogger<ThesisService>>().Object
            );
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_001_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "99bcae50-87b1-4bff-b3dc-dea03ab3215d")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 001";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1001, 
                    EnterpriseName = "NoiseData_001", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 001");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_001", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_002_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "1304edbf-c17d-48cc-9555-4005459de532")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 002";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 2, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1002, 
                    EnterpriseName = "NoiseData_002", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 002");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_002", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_003_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "05b62d65-b510-4ed8-89c9-4d2674d8534f")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 003";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 3, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1003, 
                    EnterpriseName = "NoiseData_003", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 003");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_003", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_004_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "78b5a36c-f8e8-4387-9f6e-ec9049cafddb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 004";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 4, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1004, 
                    EnterpriseName = "NoiseData_004", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 004");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_004", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_005_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "84546290-8960-4dd4-aba9-6bc139a1f148")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 005";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 5, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1005, 
                    EnterpriseName = "NoiseData_005", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 005");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_005", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_006_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "38506af2-8a04-4b1a-ac96-8cfd7e59eaa5")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 006";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 6, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1006, 
                    EnterpriseName = "NoiseData_006", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 006");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_006", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_007_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "ac00a316-ff45-47c4-be18-e479d4bbcd30")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 007";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 7, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1007, 
                    EnterpriseName = "NoiseData_007", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 007");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_007", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_008_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "ad5db55b-f30d-4efd-9fb3-760b079af60b")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 008";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 8, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1008, 
                    EnterpriseName = "NoiseData_008", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 008");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_008", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_009_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "d2d2b83f-e75d-48a6-8683-845b763d8b63")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 009";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 9, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1009, 
                    EnterpriseName = "NoiseData_009", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 009");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_009", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_010_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "66a8ca0f-f435-4e4e-9cc1-149077a86f83")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 010";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 10, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1010, 
                    EnterpriseName = "NoiseData_010", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 010");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_010", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_011_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "9cb909d3-1a9d-4acc-a4b3-d939ac573ded")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 011";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 11, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1011, 
                    EnterpriseName = "NoiseData_011", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 011");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_011", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_012_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "c37cdcdd-0ac2-4d66-8150-b1fbdfc902d3")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 012";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 12, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1012, 
                    EnterpriseName = "NoiseData_012", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 012");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_012", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_013_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "fc651e29-8ac2-46e2-a738-b0e3bd93fe1f")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 013";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 13, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1013, 
                    EnterpriseName = "NoiseData_013", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 013");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_013", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_014_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "5524eddc-c7c0-48b5-b7e4-fccc6d0d46d5")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 014";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 14, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1014, 
                    EnterpriseName = "NoiseData_014", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 014");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_014", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_015_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "1e41788e-93e9-44fc-bb99-4bc68414d1e9")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 015";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 15, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1015, 
                    EnterpriseName = "NoiseData_015", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 015");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_015", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_016_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "d2ce6525-2f10-455f-b4c1-682b869a99b6")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 016";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 16, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1016, 
                    EnterpriseName = "NoiseData_016", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 016");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_016", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_017_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "feff89b2-e19f-43d9-a939-2b0fa2cd9405")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 017";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 17, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1017, 
                    EnterpriseName = "NoiseData_017", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 017");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_017", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_018_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "e833dd91-c5f4-4ae5-af9d-8134030152ab")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 018";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 18, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1018, 
                    EnterpriseName = "NoiseData_018", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 018");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_018", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_019_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "f2afdba5-1bd4-49f6-8749-2cb6ea07fd8a")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 019";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 19, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1019, 
                    EnterpriseName = "NoiseData_019", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 019");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_019", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_020_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "b530769f-29ef-4489-a84b-ecf5df37523b")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 020";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 20, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1020, 
                    EnterpriseName = "NoiseData_020", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 020");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_020", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_021_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "04e1983e-026c-4695-a232-4581e5842a33")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 021";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 21, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1021, 
                    EnterpriseName = "NoiseData_021", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 021");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_021", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_022_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "15e48b75-4e08-4dd7-ac0c-00b126eaa18c")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 022";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 22, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1022, 
                    EnterpriseName = "NoiseData_022", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 022");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_022", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_023_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "42537940-78cd-4cf9-889d-6e3c1bc6732b")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 023";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 23, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1023, 
                    EnterpriseName = "NoiseData_023", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 023");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_023", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_024_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "d9ba0acf-1eb0-4a11-b13a-b56110f4c8b3")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 024";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 24, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1024, 
                    EnterpriseName = "NoiseData_024", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 024");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_024", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_025_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "64dbeebb-5283-43d7-9c28-b076aaffd175")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 025";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 25, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1025, 
                    EnterpriseName = "NoiseData_025", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 025");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_025", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_026_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "14c7ad19-461b-4603-b9d0-ca8425f11ecc")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 026";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 26, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1026, 
                    EnterpriseName = "NoiseData_026", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 026");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_026", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_027_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "db155890-2886-4181-b132-a190e9103c98")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 027";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 27, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1027, 
                    EnterpriseName = "NoiseData_027", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 027");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_027", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_028_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "6e504815-ad59-499d-8f00-b131594fdc7f")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 028";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 28, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1028, 
                    EnterpriseName = "NoiseData_028", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 028");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_028", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_029_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "b3a475cd-5c3a-44ba-b7fa-863f1bb80ff9")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 029";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 29, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1029, 
                    EnterpriseName = "NoiseData_029", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 029");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_029", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_030_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "1be90e98-f855-4937-8b88-e6b5303f7fbf")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 030";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 30, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1030, 
                    EnterpriseName = "NoiseData_030", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 030");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_030", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_031_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "4aea85d5-8447-48c2-8de2-132ba8be100f")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 031";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 31, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1031, 
                    EnterpriseName = "NoiseData_031", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 031");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_031", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_032_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "2d409be5-7e76-4da3-9bca-499cf0cb33f7")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 032";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 32, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1032, 
                    EnterpriseName = "NoiseData_032", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 032");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_032", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_033_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "3f64427d-3cfa-4b03-86d0-79e45bb15caa")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 033";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 33, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1033, 
                    EnterpriseName = "NoiseData_033", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 033");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_033", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_034_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "86c59624-f914-4cd3-9bab-2949cba5ce79")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 034";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 34, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1034, 
                    EnterpriseName = "NoiseData_034", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 034");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_034", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_035_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "071a7b21-6378-4cc0-ba5f-c54f9cfca532")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 035";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 35, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1035, 
                    EnterpriseName = "NoiseData_035", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 035");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_035", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_036_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "9108705f-b4b2-4f1e-83c9-e2fbfc249676")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 036";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 36, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1036, 
                    EnterpriseName = "NoiseData_036", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 036");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_036", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_037_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "342f88c0-9770-4f64-8321-dd0d508b790f")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 037";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 37, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1037, 
                    EnterpriseName = "NoiseData_037", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 037");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_037", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_038_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "ed0b375c-a301-4a91-aa50-dd7b5f7f8ad2")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 038";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 38, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1038, 
                    EnterpriseName = "NoiseData_038", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 038");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_038", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_039_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "77500122-cabd-492c-ac78-99afc30b2ab2")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 039";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 39, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1039, 
                    EnterpriseName = "NoiseData_039", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 039");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_039", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_040_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "07a3bf50-4bf5-44be-b9cd-32f0f6a0363a")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 040";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 40, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1040, 
                    EnterpriseName = "NoiseData_040", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 040");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_040", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_041_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "87944cd8-1d0d-419f-9593-ff25efe74b49")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 041";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 41, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1041, 
                    EnterpriseName = "NoiseData_041", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 041");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_041", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_042_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "f5ce9530-8b00-41b9-bb9e-969498d0a116")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 042";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 42, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1042, 
                    EnterpriseName = "NoiseData_042", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 042");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_042", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_043_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "83fe83ba-07fc-4fc6-884e-159d0cbd8178")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 043";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 43, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1043, 
                    EnterpriseName = "NoiseData_043", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 043");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_043", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_044_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "1c89a3f9-e719-40b3-a054-aa3a95221f38")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 044";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 44, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1044, 
                    EnterpriseName = "NoiseData_044", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 044");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_044", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_045_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "33514e24-38db-469b-b818-feb872749ed6")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 045";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 45, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1045, 
                    EnterpriseName = "NoiseData_045", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 045");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_045", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_046_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "eab1538e-c7bf-49f8-b547-f5210712b2cc")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 046";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 46, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1046, 
                    EnterpriseName = "NoiseData_046", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 046");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_046", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_047_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "d25939b1-22bc-4333-a67d-5811b3b5726e")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 047";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 47, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1047, 
                    EnterpriseName = "NoiseData_047", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 047");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_047", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_048_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "00928611-94cd-4c53-bae6-aa40fc411ace")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 048";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 48, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1048, 
                    EnterpriseName = "NoiseData_048", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 048");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_048", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_049_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "cd1aabe0-e6b8-4309-aa4a-3cf8f48fc430")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 049";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 49, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1049, 
                    EnterpriseName = "NoiseData_049", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 049");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_049", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_050_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "0e544c67-111b-4ffb-981c-2c4485681fe3")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 050";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 50, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1050, 
                    EnterpriseName = "NoiseData_050", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 050");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_050", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_051_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "7bc343c4-7739-4e70-8617-2de8094af0a8")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 051";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 51, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1051, 
                    EnterpriseName = "NoiseData_051", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 051");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_051", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_052_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "1fe3e9db-e302-486c-a555-a91e2ed2f692")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 052";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 52, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1052, 
                    EnterpriseName = "NoiseData_052", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 052");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_052", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_053_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "e132bd42-d8ef-4d35-9ec4-ef7d9a93103a")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 053";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 53, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1053, 
                    EnterpriseName = "NoiseData_053", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 053");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_053", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_054_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "069594b8-3c79-479b-bca9-5ebbc557caea")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 054";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 54, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1054, 
                    EnterpriseName = "NoiseData_054", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 054");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_054", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_055_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "20487f96-7a5a-4bf9-ba73-caad3e2e6eac")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 055";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 55, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1055, 
                    EnterpriseName = "NoiseData_055", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 055");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_055", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_056_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "85dea01b-0391-4633-a3ba-9d2c71daa5b3")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 056";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 56, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1056, 
                    EnterpriseName = "NoiseData_056", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 056");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_056", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_057_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "92655a87-5f01-4c9b-9c75-85ca52eefbba")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 057";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 57, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1057, 
                    EnterpriseName = "NoiseData_057", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 057");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_057", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_058_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "4aca8d09-c102-438b-b077-b393282defbd")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 058";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 58, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1058, 
                    EnterpriseName = "NoiseData_058", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 058");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_058", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_059_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "f9f20263-7dab-4c57-a8ac-5b440d609542")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 059";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 59, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1059, 
                    EnterpriseName = "NoiseData_059", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 059");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_059", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_060_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "b8aea287-98e8-47e4-805c-ec1ebd3eb877")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 060";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 60, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1060, 
                    EnterpriseName = "NoiseData_060", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 060");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_060", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_061_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "9119c826-caee-40df-857e-c9c3205d6b2b")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 061";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 61, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1061, 
                    EnterpriseName = "NoiseData_061", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 061");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_061", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_062_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "2c321a5f-c7fb-48e9-bea4-ddb549651afd")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 062";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 62, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1062, 
                    EnterpriseName = "NoiseData_062", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 062");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_062", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_063_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "40a96126-a8bc-45a3-889d-4df301f04f0c")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 063";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 63, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1063, 
                    EnterpriseName = "NoiseData_063", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 063");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_063", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_064_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "0c44d0ef-50b2-4da7-9bdf-300cd501d7a3")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 064";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 64, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1064, 
                    EnterpriseName = "NoiseData_064", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 064");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_064", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }
        [Fact]
        public async Task SearchEnterprisesAsync_WithDatabaseContext_Permutation_065_ReturnsCorrectly()
        {
            // Arrange - Real InMemory DB Context Isolation
            var options = new DbContextOptionsBuilder<FctmsContext>()
                .UseInMemoryDatabase(databaseName: "3c79c4dc-45a4-4e78-9933-b96c48a908d5")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using (var context = new FctmsContext(options))
            {
                // Inject real test data
                var enterpriseName = "FPT Software Component 065";
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 65, 
                    EnterpriseName = enterpriseName, 
                    CreatedAt = DateTime.UtcNow 
                });
                
                // Add noise data
                context.RegisteredEnterprises.Add(new RegisteredEnterprise 
                { 
                    Id = 1065, 
                    EnterpriseName = "NoiseData_065", 
                    CreatedAt = DateTime.UtcNow 
                });
                await context.SaveChangesAsync();

                var service = CreateServiceWithContext(context);

                // Act
                var result = await service.SearchEnterprisesAsync("Component 065");

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(enterpriseName, result);
                Assert.DoesNotContain("NoiseData_065", result);
                
                // Verify empty spaces handling are safely discarded
                var emptyResult = await service.SearchEnterprisesAsync("    ");
                Assert.Empty(emptyResult);
            }
        }    }
}
