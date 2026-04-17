using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Repositories;

namespace Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepo;
        private readonly ITeamRepository _teamRepo;
        private readonly ISemesterService _semesterService;
        private readonly IUserService _userService;

        public ChatService(IChatRepository chatRepo, ITeamRepository teamRepo, ISemesterService semesterService, IUserService userService)
        {
            _chatRepo = chatRepo;
            _teamRepo = teamRepo;
            _semesterService = semesterService;
            _userService = userService;
        }

        public async Task<ConversationDto> GetOrCreateConversationAsync(int currentUserId, int otherUserId, int semesterId)
        {
            // Ensure virtual user is promoted to real stub user before DB operations
            otherUserId = await _userService.EnsureUserExistsAsync(otherUserId);

            var conversation = await _chatRepo.GetOrCreateConversationAsync(currentUserId, otherUserId, semesterId);
            var dto = await _chatRepo.GetConversationDtoAsync(conversation.ConversationId, currentUserId);
            return dto ?? throw new Exception("Failed to retrieve conversation details");
        }

        public async Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int page, int pageSize)
        {
            return await _chatRepo.GetConversationMessagesAsync(conversationId, page, pageSize);
        }

        public async Task<List<ChatMessageDto>> GetTeamMessagesAsync(int teamId, int page, int pageSize)
        {
            return await _chatRepo.GetTeamMessagesAsync(teamId, page, pageSize);
        }

        public async Task<ChatMessageDto> SendDirectMessageAsync(int senderId, int receiverId, int semesterId, string content)
        {
            // [LIFECYCLE GUARD] Block chat if semester is Closed
            var semester = await _semesterService.GetSemesterByIdAsync(semesterId);
            if (semester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Học kỳ đã kết thúc, không thể gửi tin nhắn mới.");
            }

            // Ensure virtual user is promoted to real stub user before DB operations
            receiverId = await _userService.EnsureUserExistsAsync(receiverId);

            var conversation = await _chatRepo.GetOrCreateConversationAsync(senderId, receiverId, semesterId);
            var message = new ChatMessage
            {
                ConversationId = conversation.ConversationId,
                SenderId = senderId,
                Content = content,
                MessageType = "text",
                CreatedAt = DateTime.UtcNow
            };
            
            return await _chatRepo.SaveMessageAsync(message);
        }

        public async Task<ChatMessageDto> SendDirectMessageByConvAsync(int senderId, int conversationId, string content)
        {
            // Authorization Check
            if (!await IsUserInConversationAsync(senderId, conversationId))
            {
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của cuộc hội thoại này.");
            }

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                MessageType = "text",
                CreatedAt = DateTime.UtcNow
            };
            
            return await _chatRepo.SaveMessageAsync(message);
        }

        public async Task<ChatMessageDto> SendTeamMessageAsync(int senderId, int teamId, string content)
        {
            // Authorization Check
            if (!await IsUserInTeamAsync(senderId, teamId))
            {
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của nhóm này.");
            }

            // [LIFECYCLE GUARD] Block chat if semester is Closed
            var team = await _teamRepo.GetByIdAsync(teamId);
            var semester = await _semesterService.GetSemesterByIdAsync(team?.SemesterId ?? 0);
            if (semester?.Status == CampusConstants.SemesterStatus.Closed)
            {
                throw new InvalidOperationException("Học kỳ đã kết thúc, không thể gửi tin nhắn mới vào nhóm.");
            }

            var message = new ChatMessage
            {
                TeamId = teamId,
                SenderId = senderId,
                Content = content,
                MessageType = "text",
                CreatedAt = DateTime.UtcNow
            };
            
            return await _chatRepo.SaveMessageAsync(message);
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId, int semesterId)
        {
            return await _chatRepo.GetUserConversationsAsync(userId, semesterId);
        }

        public async Task MarkReadAsync(int userId, int? conversationId, int? teamId)
        {
            if (conversationId.HasValue)
            {
                var allowed = await IsUserInConversationAsync(userId, conversationId.Value);
                if (!allowed)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập cuộc hội thoại này.");
                }
            }

            if (teamId.HasValue)
            {
                var allowed = await IsUserInTeamAsync(userId, teamId.Value);
                if (!allowed)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập nhóm này.");
                }
            }

            await _chatRepo.MarkReadAsync(userId, conversationId, teamId);
        }

        public async Task<int> GetTotalUnreadCountAsync(int userId)
        {
            // Get current active semester
            var currentSemester = await _semesterService.GetCurrentSemesterAsync();
            var semesterId = currentSemester?.SemesterId ?? 1;
            
            return await _chatRepo.GetTotalUnreadCountAsync(userId, semesterId);
        }

        public async Task<bool> IsUserInTeamAsync(int userId, int teamId)
        {
            var team = await _teamRepo.GetByIdAsync(teamId);
            if (team == null) return false;
            
            return team.LeaderId == userId // Corrected: Include LeaderId
                   || team.Teammembers.Any(tm => tm.StudentId == userId) 
                   || team.MentorId == userId 
                   || team.MentorId2 == userId;
        }

        public async Task<bool> IsUserInConversationAsync(int userId, int conversationId)
        {
            var conversation = await _chatRepo.GetConversationDtoAsync(conversationId, userId);
            // GetConversationDtoAsync returns non-null if the user is User1Id or User2Id
            return conversation != null;
        }

        public async Task<List<int>> GetConversationParticipantsAsync(int conversationId)
        {
            var conversation = await _chatRepo.GetConversationByIdAsync(conversationId);
            if (conversation == null) return new List<int>();
            
            return new List<int> { conversation.User1Id, conversation.User2Id };
        }

        public async Task<List<int>> GetUserTeamIdsAsync(int userId)
        {
            var currentSemester = await _semesterService.GetCurrentSemesterAsync();
            var semesterId = currentSemester?.SemesterId ?? 0;
            if (semesterId == 0) return new List<int>();

            // Tech Lead optimization: Use the refactored chatRepo method which is more inclusive
            var teams = await _chatRepo.GetActiveTeamsForUserAsync(userId, semesterId);
            return teams.Select(t => t.TeamId).ToList();
        }

        public async Task<List<TeamChatInfoDto>> GetUserTeamChatListAsync(int userId, int semesterId)
        {
            return await _chatRepo.GetUserTeamChatListAsync(userId, semesterId);
        }
    }
}
