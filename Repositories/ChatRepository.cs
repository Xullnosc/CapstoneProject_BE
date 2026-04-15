using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using BusinessObjects.Models;
using DataAccess;

namespace Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly IChatDAO _chatDAO;

        public ChatRepository(IChatDAO chatDAO)
        {
            _chatDAO = chatDAO;
        }

        public async Task<ChatConversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _chatDAO.GetConversationByIdAsync(conversationId);
        }

        public async Task<ChatConversation> GetOrCreateConversationAsync(int currentUserId, int otherUserId, int semesterId)
        {
            int u1 = Math.Min(currentUserId, otherUserId);
            int u2 = Math.Max(currentUserId, otherUserId);

            var conversation = await _chatDAO.GetConversationAsync(u1, u2, semesterId);
            if (conversation == null)
            {
                conversation = await _chatDAO.CreateConversationAsync(u1, u2, semesterId);
            }
            return conversation;
        }

        public async Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int page, int pageSize)
        {
            var messages = await _chatDAO.GetMessagesByConversationAsync(conversationId, page, pageSize);
            return messages.Select(MapToDto).ToList();
        }

        public async Task<List<ChatMessageDto>> GetTeamMessagesAsync(int teamId, int page, int pageSize)
        {
            var messages = await _chatDAO.GetMessagesByTeamAsync(teamId, page, pageSize);
            return messages.Select(MapToDto).ToList();
        }

        public async Task<List<ChatMessageDto>> GetLastMessagesByTeamIdsAsync(List<int> teamIds)
        {
            var messages = await _chatDAO.GetLastMessagesByTeamIdsAsync(teamIds);
            return messages.Select(MapToDto).ToList();
        }

        public async Task<ChatMessageDto> SaveMessageAsync(ChatMessage message)
        {
            var saved = await _chatDAO.SaveMessageAsync(message);
            
            // Re-map to DTO with sender info (sender is usually already in context from EF)
            return MapToDto(saved);
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId, int semesterId)
        {
            var conversations = await _chatDAO.GetConversationsByUserAsync(userId, semesterId);
            var results = new List<ConversationDto>();

            foreach (var c in conversations)
            {
                var otherUser = c.User1Id == userId ? c.User2 : c.User1;
                var lastMsg = await _chatDAO.GetLastMessageByConversationAsync(c.ConversationId);
                var unreadCount = await _chatDAO.GetUnreadCountAsync(userId, c.ConversationId, null);

                results.Add(new ConversationDto(
                    c.ConversationId,
                    otherUser.UserId,
                    otherUser.FullName,
                    string.IsNullOrWhiteSpace(otherUser.Avatar)
                        ? $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(otherUser.FullName)}&background=random&color=fff"
                        : otherUser.Avatar,
                    lastMsg?.Content,
                    lastMsg?.CreatedAt,
                    unreadCount
                ));
            }

            return results;
        }

        public async Task MarkReadAsync(int userId, int? conversationId, int? teamId)
        {
            await _chatDAO.MarkReadAsync(userId, conversationId, teamId);
        }

        public async Task<int> GetUnreadCountAsync(int userId, int? conversationId, int? teamId)
        {
            return await _chatDAO.GetUnreadCountAsync(userId, conversationId, teamId);
        }

        public async Task<int> GetTotalUnreadCountAsync(int userId, int semesterId)
        {
            return await _chatDAO.GetTotalUnreadCountAsync(userId, semesterId);
        }

        public async Task<List<UserSkill>> GetUserSkillsAsync(int userId)
        {
            return await _chatDAO.GetUserSkillsAsync(userId);
        }

        public async Task ReplaceUserSkillsAsync(int userId, List<UserSkill> newSkills)
        {
            await _chatDAO.ReplaceUserSkillsAsync(userId, newSkills);
        }

        public async Task<List<TeamChatInfoDto>> GetUserTeamChatListAsync(int userId, int semesterId)
        {
            var team = await _chatDAO.GetActiveTeamByStudentIdAsync(userId, semesterId);
            var results = new List<TeamChatInfoDto>();

            if (team != null)
            {
                var lastMsg = await _chatDAO.GetLastMessageByTeamAsync(team.TeamId);
                var unreadCount = await _chatDAO.GetUnreadCountAsync(userId, null, team.TeamId);

                results.Add(new TeamChatInfoDto(
                    team.TeamId,
                    team.TeamName,
                    team.TeamAvatar,
                    lastMsg?.Content,
                    lastMsg?.CreatedAt,
                    unreadCount
                ));
            }

            return results;
        }

        public async Task<ConversationDto?> GetConversationDtoAsync(int conversationId, int userId)
        {
            var c = await _chatDAO.GetConversationByIdAsync(conversationId);
            if (c == null) return null;

            if (c.User1Id != userId && c.User2Id != userId) return null;

            var otherUser = c.User1Id == userId ? c.User2 : c.User1;
            var lastMsg = await _chatDAO.GetLastMessageByConversationAsync(c.ConversationId);
            var unreadCount = await _chatDAO.GetUnreadCountAsync(userId, c.ConversationId, null);

            return new ConversationDto(
                c.ConversationId,
                otherUser.UserId,
                otherUser.FullName,
                string.IsNullOrWhiteSpace(otherUser.Avatar)
                    ? $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(otherUser.FullName)}&background=random&color=fff"
                    : otherUser.Avatar,
                lastMsg?.Content,
                lastMsg?.CreatedAt,
                unreadCount
            );
        }

        private ChatMessageDto MapToDto(ChatMessage m)
        {
            return new ChatMessageDto(
                m.MessageId,
                m.SenderId,
                m.Sender?.FullName ?? "Unknown",
                string.IsNullOrWhiteSpace(m.Sender?.Avatar)
                    ? $"https://ui-avatars.com/api/?name={System.Net.WebUtility.UrlEncode(m.Sender?.FullName ?? "U")}&background=random&color=fff"
                    : m.Sender?.Avatar,
                m.Content,
                m.MessageType,
                m.AttachmentUrl,
                m.AttachmentName,
                m.CreatedAt,
                m.ConversationId,
                m.TeamId
            );
        }
    }
}
