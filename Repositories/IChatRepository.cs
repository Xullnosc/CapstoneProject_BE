using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using BusinessObjects.DTOs;

namespace Repositories
{
    public interface IChatRepository
    {
        Task<ChatConversation?> GetConversationByIdAsync(int conversationId);
        Task<ChatConversation> GetOrCreateConversationAsync(int currentUserId, int otherUserId, int semesterId);
        Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int page, int pageSize);
        Task<List<ChatMessageDto>> GetTeamMessagesAsync(int teamId, int page, int pageSize);
        Task<List<ChatMessageDto>> GetLastMessagesByTeamIdsAsync(List<int> teamIds);
        Task<ChatMessageDto> SaveMessageAsync(ChatMessage message);
        Task<List<ConversationDto>> GetUserConversationsAsync(int userId, int semesterId);
        Task<ConversationDto?> GetConversationDtoAsync(int conversationId, int userId);
        Task MarkReadAsync(int userId, int? conversationId, int? teamId);
        Task<int> GetUnreadCountAsync(int userId, int? conversationId, int? teamId);
        Task<int> GetTotalUnreadCountAsync(int userId, int semesterId);
        Task<List<UserSkill>> GetUserSkillsAsync(int userId);
        Task ReplaceUserSkillsAsync(int userId, List<UserSkill> newSkills);
        Task<List<TeamChatInfoDto>> GetUserTeamChatListAsync(int userId, int semesterId);
    }
}
