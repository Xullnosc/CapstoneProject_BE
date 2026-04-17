using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Services;
using BusinessObjects.DTOs;

namespace CapstoneProject_BE.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private static readonly HashSet<int> _onlineUsers = new HashSet<int>();

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                lock (_onlineUsers)
                {
                    _onlineUsers.Add(userId);
                }

                // Notify all about the updated online list
                await Clients.All.SendAsync("UpdateOnlineUsers", _onlineUsers);

                // Join personal group for 1-1 message notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Join all teams the user belongs to
                var teamIds = await _chatService.GetUserTeamIdsAsync(userId);
                foreach (var teamId in teamIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Team_{teamId}");
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                lock (_onlineUsers)
                {
                    _onlineUsers.Remove(userId);
                }
                
                // Notify all that a user disconnected
                await Clients.All.SendAsync("UpdateOnlineUsers", _onlineUsers);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDirectMessage(int conversationId, string content)
        {
            var senderIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(senderIdStr, out int senderId))
            {
                var messageDto = await _chatService.SendDirectMessageByConvAsync(senderId, conversationId, content);
                
                // Get participants to notify them individually (Realtime)
                var participants = await _chatService.GetConversationParticipantsAsync(conversationId);
                foreach (var uid in participants)
                {
                    // 1. Send the message
                    await Clients.Group($"User_{uid}").SendAsync("ReceiveMessage", messageDto);
                    
                    // 2. Real-time Header Sync: Notify recipients to refresh their unread count
                    if (uid != senderId)
                    {
                        var unreadCount = await _chatService.GetTotalUnreadCountAsync(uid);
                        await Clients.Group($"User_{uid}").SendAsync("UpdateUnreadCount", unreadCount);
                    }
                }
            }
        }

        public async Task SendTeamMessage(int teamId, string content)
        {
            var senderIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(senderIdStr, out int senderId))
            {
                // Check if user is in team
                if (await _chatService.IsUserInTeamAsync(senderId, teamId))
                {
                    var messageDto = await _chatService.SendTeamMessageAsync(senderId, teamId, content);
                    
                    // Broadcast to team group members
                    // Note: Ideally we want individual 'User_{uid}' notifications too for unread count,
                    // but for team chat we join the Team_{id} group.
                    await Clients.Group($"Team_{teamId}").SendAsync("ReceiveMessage", messageDto);
                    
                    // TODO: In a large system, we'd emit UpdateUnreadCount to individual users.
                    // For now, let's keep it consistent.
                }
                else
                {
                    throw new HubException("You are not a member of this team.");
                }
            }
        }

        public async Task MarkAsRead(int? conversationId, int? teamId)
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                await _chatService.MarkReadAsync(userId, conversationId, teamId);
                
                // Real-time Header Sync for current user (multi-tab sync)
                var unreadCount = await _chatService.GetTotalUnreadCountAsync(userId);
                await Clients.Group($"User_{userId}").SendAsync("UpdateUnreadCount", unreadCount);
            }
        }

        public async Task JoinConversation(int conversationId)
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                if (await _chatService.IsUserInConversationAsync(userId, conversationId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
                }
                else
                {
                    throw new HubException("Bạn không có quyền tham gia cuộc hội thoại này.");
                }
            }
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
        }
    }
}
