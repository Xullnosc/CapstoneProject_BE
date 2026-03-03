using System.Threading.Tasks;
using BusinessObjects.Models;
using DataAccess;
using Org.BouncyCastle.Tls;
using Repositories;
using System.Reflection;
using System.Threading.Tasks;

namespace Services
{
    public class ArchivingService : IArchivingService
    {
        private readonly IArchivingRepository _archivingRepository;
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IRedisService _redisService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly TimeSpan _archivedTtl;

        public ArchivingService(
            IArchivingRepository archivingRepository, 
            IWhitelistRepository whitelistRepository, 
            ITeamRepository teamRepository, 
            IRedisService redisService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _whitelistRepository = whitelistRepository;
            _archivingRepository = archivingRepository;
            _teamRepository = teamRepository;
            _redisService = redisService;
            _configuration = configuration;
            
            var ttlStr = _configuration["RedisSettings:ArchivedTTLMinutes"];
            int ttlMinutes;
            if (!int.TryParse(ttlStr, out ttlMinutes)) ttlMinutes = 1440; // Default 24h
            ttlMinutes = System.Math.Max(1, ttlMinutes); // Ensure a positive TTL
            _archivedTtl = System.TimeSpan.FromMinutes(ttlMinutes);
        }

        public async Task ArchiveSemesterAsync(int semesterId)
        {
            var whitelistsToArchive = await _whitelistRepository.GetBySemesterIdAsync(semesterId);
            if (whitelistsToArchive.Any())
            {
                var archivedWhitelists = whitelistsToArchive.Select(w => new ArchivedWhitelist
                {
                    OriginalWhitelistId = w.WhitelistId,
                    StudentCode = w.StudentCode,
                    Email = w.Email,
                    FullName = w.FullName,
                    RoleId = w.RoleId,
                    Campus = w.Campus,
                    SemesterId = semesterId,
                    ArchivedAt = DateTime.UtcNow
                });
                await _archivingRepository.ArchiveWhitelistsAsync(archivedWhitelists);
                await _whitelistRepository.DeleteRangeAsync(whitelistsToArchive);
            }
            var teamsToArchive = await _teamRepository.GetForArchivingAsync(semesterId);
            if (teamsToArchive.Any())
            {
                var archivedTeams = teamsToArchive.Select(t => new ArchivedTeam
                {
                    OriginalTeamId = t.TeamId,
                    TeamCode = t.TeamCode,
                    TeamName = t.TeamName,
                    SemesterId = t.SemesterId,
                    LeaderId = t.LeaderId,
                    Status = t.Status,
                    ArchivedAt = DateTime.UtcNow,
                    // Simple serialization for snapshot
                    JsonData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Members = t.Teammembers.Select(m => new { m.StudentId, m.Role }),
                        TopicId = 0 // Placeholder until Topic implemented
                    })
                });

                await _archivingRepository.ArchiveTeamsAsync(archivedTeams);
                await _teamRepository.DeleteRangeAsync(teamsToArchive);
            }
        }

        public async Task ArchiveTeamAsync(Team team)
        {
            var archivedTeam = new ArchivedTeam
            {
                OriginalTeamId = team.TeamId,
                TeamCode = team.TeamCode,
                TeamName = team.TeamName,
                SemesterId = team.SemesterId,
                LeaderId = team.LeaderId,
                Status = "Disbanded",
                ArchivedAt = DateTime.UtcNow,
                JsonData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Members = team.Teammembers.Select(m => new { m.StudentId, m.Role }),
                    TopicId = 0
                })
            };

            await _archivingRepository.ArchiveTeamAsync(archivedTeam);
            await _teamRepository.DeleteTeamAsync(team);
        }

        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterAsync(int semesterId)
        {
            string semesterKey = $"fctms:archivedTeam:semester:{semesterId}";
            var teamIds = await _redisService.SetMembersAsync(semesterKey);
            if (teamIds.Any())
            {
                var teams = new List<ArchivedTeam>();
                foreach (var id in teamIds)
                {
                    var team = await _redisService
                        .GetObjectAsync<ArchivedTeam>($"fctms:archivedTeam:id:{id}");
                    if (team != null)
                        teams.Add(team);
                }
                if (teams.Any())
                    return teams;
            }

            // Cache miss → query DB
            var archivedTeams = await _archivingRepository
                .GetArchivedTeamsBySemesterAsync(semesterId);
            foreach (var team in archivedTeams)
            {
                await _redisService.SetObjectAsync(
                    $"fctms:archivedTeam:id:{team.ArchivedTeamId}",
                    team,
                    TimeSpan.FromMinutes(60)); // Use longer TTL for archived data
                await _redisService.SetAddAsync(semesterKey, team.ArchivedTeamId.ToString());
            }

            await _redisService.ExpireAsync(
                semesterKey,
                TimeSpan.FromMinutes(60));
            return archivedTeams;
        }

        public async Task<List<ArchivedTeam>> GetArchivedTeamsBySemesterIdsAsync(
            List<int> semesterIds
        )
        {
            var result = new List<ArchivedTeam>();
            var missingSemesterIds = new List<int>();
            foreach (var semesterId in semesterIds)
            {
                var semesterKey = $"fctms:archivedTeam:semester:{semesterId}";
                var teamIds = await _redisService.SetMembersAsync(semesterKey);

                if (teamIds.Any())
                {
                    var teams = new List<ArchivedTeam>();
                    foreach (var id in teamIds)
                    {
                        var team = await _redisService
                            .GetObjectAsync<ArchivedTeam>($"fctms:archivedTeam:id:{id}");
                        if (team != null)
                            teams.Add(team);
                    }
                    if (teams.Any())
                        result.AddRange(teams);
                }
                else
                {
                    missingSemesterIds.Add(semesterId);
                }
            }

            if (missingSemesterIds.Any())
            {
                var dbResults = await _archivingRepository.GetArchivedTeamsBySemesterIdsAsync(missingSemesterIds);
                var grouped = dbResults
                    .GroupBy(x => x.SemesterId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                foreach (var kvp in grouped)
                {
                    var semesterId = kvp.Key;
                    var teams = kvp.Value;
                    var semesterKey = $"fctms:archivedTeam:semester:{semesterId}";
                    foreach (var team in teams)
                    {
                        await _redisService.SetObjectAsync(
                            $"fctms:archivedTeam:id:{team.ArchivedTeamId}",
                            team,
                            TimeSpan.FromMinutes(60));
                        await _redisService.SetAddAsync(
                            semesterKey,
                            team.ArchivedTeamId.ToString());
                        result.Add(team);
                    }

                    await _redisService.ExpireAsync(
                        semesterKey,
                        TimeSpan.FromMinutes(60));
                }
            }
            return result;
        }

        public async Task<List<ArchivedWhitelist>> GetArchivedWhitelistsBySemesterIdsAsync(List<int> semesterIds)
        {
            var result = new List<ArchivedWhitelist>();
            var missingSemesterIds = new List<int>();

            foreach (var semesterId in semesterIds)
            {
                var semesterKey = $"fctms:archivedWhitelist:semester:{semesterId}";
                var whitelistIds = await _redisService.SetMembersAsync(semesterKey);

                if (whitelistIds.Any())
                {
                    var items = new List<ArchivedWhitelist>();
                    foreach (var id in whitelistIds)
                    {
                        var item = await _redisService.GetObjectAsync<ArchivedWhitelist>($"fctms:archivedWhitelist:id:{id}");
                        if (item != null)
                            items.Add(item);
                    }
                    if (items.Any())
                        result.AddRange(items);
                }
                else
                {
                    missingSemesterIds.Add(semesterId);
                }
            }

            if (missingSemesterIds.Any())
            {
                var dbResults = await _archivingRepository.GetArchivedWhitelistsBySemesterIdsAsync(missingSemesterIds);
                var grouped = dbResults.GroupBy(x => x.SemesterId).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var kvp in grouped)
                {
                    var semesterId = kvp.Key;
                    var items = kvp.Value;
                    var semesterKey = $"fctms:archivedWhitelist:semester:{semesterId}";
                    foreach (var item in items)
                    {
                        await _redisService.SetObjectAsync($"fctms:archivedWhitelist:id:{item.ArchivedWhitelistId}", item, TimeSpan.FromMinutes(60));
                        await _redisService.SetAddAsync(semesterKey, item.ArchivedWhitelistId.ToString());
                        result.Add(item);
                    }

                    await _redisService.ExpireAsync(semesterKey, TimeSpan.FromMinutes(60));
                }
            }

            return result;
        }

        public async Task<List<ArchivedTeam>> GetAllArchivedTeamsAsync()
        {
            return await _archivingRepository.GetAllArchivedTeamsAsync();
        }
    }
}
