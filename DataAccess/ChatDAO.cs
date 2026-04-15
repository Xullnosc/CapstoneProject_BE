using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ChatDAO : IChatDAO
    {
        private readonly FctmsContext _context;

        public ChatDAO(FctmsContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        // Conversations (DM)
        // ─────────────────────────────────────────────────────────────
        public async Task<ChatConversation?> GetConversationAsync(int user1Id, int user2Id, int semesterId)
        {
            return await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id && c.SemesterId == semesterId);
        }

        public async Task<ChatConversation> CreateConversationAsync(int user1Id, int user2Id, int semesterId)
        {
            var conversation = new ChatConversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
                SemesterId = semesterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<ChatConversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.ChatConversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<List<ChatConversation>> GetConversationsByUserAsync(int userId, int semesterId)
        {
            return await _context.ChatConversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => (c.User1Id == userId || c.User2Id == userId) && 
                            c.SemesterId == semesterId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Messages
        // ─────────────────────────────────────────────────────────────
        public async Task<List<ChatMessage>> GetMessagesByConversationAsync(int conversationId, int page, int pageSize)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetMessagesByTeamAsync(int teamId, int page, int pageSize)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.TeamId == teamId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            
            // Update conversation UpdatedAt timestamp if it's a DM
            if (message.ConversationId.HasValue)
            {
                var conv = await _context.ChatConversations.FindAsync(message.ConversationId.Value);
                if (conv != null) conv.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            // Explicitly load Sender info for the DTO mapping
            await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
            
            return message;
        }

        public async Task<ChatMessage?> GetLastMessageByConversationAsync(int conversationId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ChatMessage?> GetLastMessageByTeamAsync(int teamId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.TeamId == teamId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChatMessage>> GetLastMessagesByTeamIdsAsync(List<int> teamIds)
        {
            if (teamIds == null || !teamIds.Any()) return new List<ChatMessage>();

            // Use group by to get the latest message for each team
            var latestMessages = await _context.ChatMessages
                .Where(m => m.TeamId != null && teamIds.Contains(m.TeamId.Value))
                .GroupBy(m => m.TeamId)
                .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
                .ToListAsync();

            return latestMessages.Where(m => m != null).ToList()!;
        }

        // ─────────────────────────────────────────────────────────────
        // Read Status
        // ─────────────────────────────────────────────────────────────
        public async Task MarkReadAsync(int userId, int? conversationId, int? teamId)
        {
            var status = await _context.ChatReadStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ConversationId == conversationId && s.TeamId == teamId);

            if (status == null)
            {
                status = new ChatReadStatus
                {
                    UserId = userId,
                    ConversationId = conversationId,
                    TeamId = teamId,
                    LastReadAt = DateTime.UtcNow
                };
                _context.ChatReadStatuses.Add(status);
            }
            else
            {
                status.LastReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId, int? conversationId, int? teamId)
        {
            var lastRead = await _context.ChatReadStatuses
                .Where(s => s.UserId == userId && s.ConversationId == conversationId && s.TeamId == teamId)
                .Select(s => s.LastReadAt)
                .FirstOrDefaultAsync();

            var query = _context.ChatMessages.AsQueryable();
            if (conversationId.HasValue) query = query.Where(m => m.ConversationId == conversationId);
            if (teamId.HasValue) query = query.Where(m => m.TeamId == teamId);

            return await query.CountAsync(m => m.SenderId != userId && (lastRead == default || m.CreatedAt > lastRead));
        }

        public async Task<int> GetTotalUnreadCountAsync(int userId, int semesterId)
        {
            // Count unread DM messages for conversations in this semester.
            var dmUnread = await (from c in _context.ChatConversations
                                  where (c.User1Id == userId || c.User2Id == userId) && c.SemesterId == semesterId
                                  join s in _context.ChatReadStatuses 
                                    on new { Id = (int?)c.ConversationId, Uid = userId } 
                                    equals new { Id = s.ConversationId, Uid = s.UserId } into readStatus
                                  from rs in readStatus.DefaultIfEmpty()
                                  select _context.ChatMessages.Count(m => m.ConversationId == c.ConversationId 
                                                                         && m.SenderId != userId 
                                                                         && (rs == null || m.CreatedAt > rs.LastReadAt)))
                                 .SumAsync();

            // Count unread team messages for teams where user is either student member or mentor.
            var userTeamIds = await _context.Teams
                .Where(t =>
                    t.SemesterId == semesterId
                    && (t.MentorId == userId
                        || t.MentorId2 == userId
                        || t.Teammembers.Any(tm => tm.StudentId == userId)))
                .Select(t => t.TeamId)
                .Distinct()
                .ToListAsync();

            int teamUnread = 0;
            foreach (var teamId in userTeamIds)
            {
                var lastRead = await _context.ChatReadStatuses
                    .Where(s => s.UserId == userId && s.TeamId == teamId && s.ConversationId == null)
                    .Select(s => s.LastReadAt)
                    .FirstOrDefaultAsync();

                teamUnread += await _context.ChatMessages.CountAsync(m =>
                    m.TeamId == teamId
                    && m.SenderId != userId
                    && (lastRead == default || m.CreatedAt > lastRead));
            }

            return dmUnread + teamUnread;
        }

        // ─────────────────────────────────────────────────────────────
        // User Skills
        // ─────────────────────────────────────────────────────────────
        public async Task<List<UserSkill>> GetUserSkillsAsync(int userId)
        {
            return await _context.UserSkills
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task ReplaceUserSkillsAsync(int userId, List<UserSkill> skills)
        {
            var existing = await _context.UserSkills.Where(s => s.UserId == userId).ToListAsync();
            _context.UserSkills.RemoveRange(existing);
            _context.UserSkills.AddRange(skills);
            await _context.SaveChangesAsync();
        }

        public async Task<Team?> GetActiveTeamByStudentIdAsync(int userId, int semesterId)
        {
            return await _context.Teammembers
                .Include(tm => tm.Team)
                .Where(tm => tm.StudentId == userId && tm.Team.SemesterId == semesterId)
                .Select(tm => tm.Team)
                .FirstOrDefaultAsync();
        }
    }
}
