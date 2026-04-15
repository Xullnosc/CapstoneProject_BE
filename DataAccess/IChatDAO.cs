using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Models;
using BusinessObjects.DTOs;

namespace DataAccess
{
    public interface IChatDAO
    {
        // Conversations (DM)
        Task<ChatConversation?> GetConversationAsync(int user1Id, int user2Id, int semesterId);
        Task<ChatConversation> CreateConversationAsync(int user1Id, int user2Id, int semesterId);
        Task<List<ChatConversation>> GetConversationsByUserAsync(int userId, int semesterId);
        Task<ChatConversation?> GetConversationByIdAsync(int conversationId);

        // Messages
        Task<List<ChatMessage>> GetMessagesByConversationAsync(int conversationId, int page, int pageSize);
        Task<List<ChatMessage>> GetMessagesByTeamAsync(int teamId, int page, int pageSize);
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);
        Task<ChatMessage?> GetLastMessageByConversationAsync(int conversationId);
        Task<ChatMessage?> GetLastMessageByTeamAsync(int teamId);
        Task<List<ChatMessage>> GetLastMessagesByTeamIdsAsync(List<int> teamIds);

        // Read Status
        Task MarkReadAsync(int userId, int? conversationId, int? teamId);
        Task<int> GetUnreadCountAsync(int userId, int? conversationId, int? teamId);
        Task<int> GetTotalUnreadCountAsync(int userId, int semesterId);
        Task<Team?> GetActiveTeamByStudentIdAsync(int userId, int semesterId);

        // User Skills
        Task<List<UserSkill>> GetUserSkillsAsync(int userId);
        Task ReplaceUserSkillsAsync(int userId, List<UserSkill> skills);
    }
}
