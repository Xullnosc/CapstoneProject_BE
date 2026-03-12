using AutoMapper;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.Extensions.Logging;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class ChecklistService : IChecklistService
    {
        private readonly IChecklistRepository _repository;
        private readonly IMapper _mapper;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ChecklistService> _logger;

        public ChecklistService(
            IChecklistRepository repository,
            IMapper mapper,
            ISemesterRepository semesterRepository,
            ITeamRepository teamRepository,
            INotificationService notificationService,
            ILogger<ChecklistService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _semesterRepository = semesterRepository;
            _teamRepository = teamRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<ChecklistDTO>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return _mapper.Map<List<ChecklistDTO>>(list);
        }

        public async Task<ChecklistDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ChecklistDTO>(entity);
        }

        public async Task<ChecklistDTO> CreateAsync(ChecklistCreateDTO dto)
        {
            var entity = _mapper.Map<Checklist>(dto);
            entity = await _repository.AddAsync(entity);

            await NotifyActiveSemesterUsersAsync(
                "Checklist created",
                $"A new checklist item was added: {entity.Title}",
                entity.ChecklistId);

            return _mapper.Map<ChecklistDTO>(entity);
        }

        public async Task UpdateAsync(int id, ChecklistUpdateDTO dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Checklist with id {id} not found.");

            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);

            await NotifyActiveSemesterUsersAsync(
                "Checklist updated",
                $"Checklist item was updated: {entity.Title}",
                entity.ChecklistId);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"Checklist with id {id} not found.");
            await _repository.DeleteAsync(id);

            await NotifyActiveSemesterUsersAsync(
                "Checklist removed",
                $"Checklist item was removed: {entity.Title}",
                entity.ChecklistId);
        }

        private async Task NotifyActiveSemesterUsersAsync(string title, string message, int checklistId)
        {
            try
            {
                var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
                if (currentSemester == null)
                {
                    return;
                }

                var teams = await _teamRepository.GetBySemesterAsync(currentSemester.SemesterId);
                var recipientIds = teams
                    .SelectMany(team =>
                        team.Teammembers.Select(member => member.StudentId)
                            .Concat(team.MentorId.HasValue ? new[] { team.MentorId.Value } : Enumerable.Empty<int>())
                            .Concat(team.MentorId2.HasValue ? new[] { team.MentorId2.Value } : Enumerable.Empty<int>()))
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (recipientIds.Count == 0)
                {
                    return;
                }

                await _notificationService.CreateBulkNotificationsAsync(
                    recipientIds,
                    NotificationType.ChecklistUpdate.ToString(),
                    title,
                    message,
                    "Checklist",
                    checklistId,
                    sendEmail: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify users for checklist event. ChecklistId: {ChecklistId}", checklistId);
            }
        }
    }
}
