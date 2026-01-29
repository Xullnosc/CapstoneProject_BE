using BusinessObjects.DTOs;
using BusinessObjects.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Repositories;

namespace Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IArchivingService _archivingService;
        private readonly IMapper _mapper;

        public SemesterService(ISemesterRepository semesterRepository, IArchivingService archivingService, IMapper mapper)
        {
            _semesterRepository = semesterRepository;
            _archivingService = archivingService;
            _mapper = mapper;
        }

        public async Task<List<SemesterDTO>> GetAllSemestersAsync()
        {
            var semesters = await _semesterRepository.GetAllSemestersAsync();
            var semesterDTOs = _mapper.Map<List<SemesterDTO>>(semesters);

            // Fetch and merge archived teams manually - Optimized: Filter by Semester IDs
            // Fetch and merge archived teams manually - Optimized: Filter by Semester IDs
            var semesterIds = semesterDTOs.Select(s => s.SemesterId).ToList();
            var allArchivedTeams = await _archivingService.GetArchivedTeamsBySemesterIdsAsync(semesterIds);
            
            var archivedTeamsBySemester = allArchivedTeams
                .GroupBy(x => x.SemesterId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dto in semesterDTOs)
            {
                // Add Active Count (mapped by AutoMapper) + Archived Count
                int archivedCount = 0;
                if (archivedTeamsBySemester.TryGetValue(dto.SemesterId, out var archived))
                {
                    archivedCount = archived.Count;
                }
                
                dto.TeamCount += archivedCount;
                
                // CRITICAL OPTIMIZATION: Clear the Teams list for the Dashboard view.
                // The Dashboard only needs the Count. Sending the list of all teams is wasteful.
                dto.Teams = new List<TeamSimpleDTO>(); 
            }

            return semesterDTOs;
        }

        public async Task<SemesterDTO?> GetSemesterByIdAsync(int id)
        {
            var semester = await _semesterRepository.GetSemesterByIdAsync(id);
            var dto = _mapper.Map<SemesterDTO>(semester);

            if (dto != null)
            {
                // Fetch and merge archived teams manually
                var archived = await _archivingService.GetArchivedTeamsBySemesterAsync(id);
                if (archived != null && archived.Any())
                {
                    var archivedDTOs = _mapper.Map<List<TeamSimpleDTO>>(archived);
                    dto.Teams.AddRange(archivedDTOs);
                }
            }
            return dto;
        }

        public async Task<SemesterDTO> CreateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            ValidateSemesterLogic(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            var createdSemester = await _semesterRepository.CreateSemesterAsync(semester);
            return _mapper.Map<SemesterDTO>(createdSemester);
        }

        public async Task UpdateSemesterAsync(SemesterCreateDTO semesterCreateDTO)
        {
            ValidateSemesterLogic(semesterCreateDTO);

            var existing = await _semesterRepository.GetSemesterByCodeAsync(semesterCreateDTO.SemesterCode);
            if (existing != null && existing.SemesterId != semesterCreateDTO.SemesterId)
            {
                throw new System.InvalidOperationException($"Semester code '{semesterCreateDTO.SemesterCode}' already exists.");
            }

            var semester = _mapper.Map<Semester>(semesterCreateDTO);
            await _semesterRepository.UpdateSemesterAsync(semester);
        }

        private void ValidateSemesterLogic(SemesterCreateDTO dto)
        {
            var code = dto.SemesterCode.ToUpper(); // e.g., SP24
            var name = dto.SemesterName.ToLower();

            // 1. Check Format (SP/SU/FA + 2 digits)
            if (code.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^(SP|SU|FA)\\d{2}$"))
            {
                throw new ArgumentException("Semester Code must be in format SPxx, SUxx, or FAxx (e.g., SP24).");
            }

            var prefix = code.Substring(0, 2);

            // 2. Check Season Match (Code vs Name)
            if (prefix == "SP" && !name.Contains("spring")) throw new ArgumentException("Code 'SP' (Spring) requires 'Spring' in Semester Name.");
            if (prefix == "SU" && !name.Contains("summer")) throw new ArgumentException("Code 'SU' (Summer) requires 'Summer' in Semester Name.");
            if (prefix == "FA" && !name.Contains("fall")) throw new ArgumentException("Code 'FA' (Fall) requires 'Fall' in Semester Name.");
        }



        public async Task EndSemesterAsync(int id)
        {
            // Use explicit transaction scope to ensure atomicity
            using var transaction = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                var semester = await _semesterRepository.GetSemesterByIdAsync(id);
                if (semester == null)
                {
                    throw new KeyNotFoundException($"Semester with ID {id} not found.");
                }

                // 1. Mark as Inactive
                semester.IsActive = false;
                await _semesterRepository.UpdateSemesterAsync(semester);

                // 2. Archive Data
                await _archivingService.ArchiveSemesterAsync(id);

                transaction.Complete();
            }
            catch (Exception)
            {
                // Transaction will auto-rollback if not completed
                throw;
            }
        }
    }
}
