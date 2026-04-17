using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;

namespace Services
{
    public interface IChatService
    {
        Task<ConversationDto> GetOrCreateConversationAsync(int currentUserId, int otherUserId, int semesterId);
        Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int page, int pageSize);
        Task<List<ChatMessageDto>> GetTeamMessagesAsync(int teamId, int page, int pageSize);
        Task<ChatMessageDto> SendDirectMessageAsync(int senderId, int receiverId, int semesterId, string content);
        Task<ChatMessageDto> SendDirectMessageByConvAsync(int senderId, int conversationId, string content);
        Task<ChatMessageDto> SendTeamMessageAsync(int senderId, int teamId, string content);
        Task<List<ConversationDto>> GetUserConversationsAsync(int userId, int semesterId);
        Task MarkReadAsync(int userId, int? conversationId, int? teamId);
        Task<int> GetTotalUnreadCountAsync(int userId);
        
        Task<List<int>> GetConversationParticipantsAsync(int conversationId);
        // Internal helpers for SignalR
        Task<bool> IsUserInTeamAsync(int userId, int teamId);
        Task<bool> IsUserInConversationAsync(int userId, int conversationId);
        Task<List<int>> GetUserTeamIdsAsync(int userId);
        Task<List<TeamChatInfoDto>> GetUserTeamChatListAsync(int userId, int semesterId);
    }
}
