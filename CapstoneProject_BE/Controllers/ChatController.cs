using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using BusinessObjects.Interfaces;

using Microsoft.AspNetCore.SignalR;
using CapstoneProject_BE.Hubs;

namespace CapstoneProject_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ICampusContextService _campusContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            IChatService chatService, 
            ICampusContextService campusContext,
            IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _campusContext = campusContext;
            _hubContext = hubContext;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] int semesterId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conversations = await _chatService.GetUserConversationsAsync(userId, semesterId);
            return Ok(conversations);
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetTeamChats([FromQuery] int semesterId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var teams = await _chatService.GetUserTeamChatListAsync(userId, semesterId);
            return Ok(teams);
        }

        [HttpGet("get-or-create")]
        public async Task<IActionResult> GetOrCreateConversation([FromQuery] int otherUserId, [FromQuery] int semesterId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conversation = await _chatService.GetOrCreateConversationAsync(userId, otherUserId, semesterId);
            return Ok(conversation);
        }

        [HttpGet("history/{conversationId}")]
        public async Task<IActionResult> GetConversationMessages(
            int conversationId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Authorization check
            if (!await _chatService.IsUserInConversationAsync(userId, conversationId))
            {
                return StatusCode(403, new { message = "You are not a participant of this conversation." });
            }

            var messages = await _chatService.GetConversationMessagesAsync(conversationId, page, pageSize);
            return Ok(messages);
        }

        [HttpGet("team-history/{teamId}")]
        public async Task<IActionResult> GetTeamMessages(
            int teamId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Authorization check
            if (!await _chatService.IsUserInTeamAsync(userId, teamId))
            {
                return StatusCode(403, new { message = "You are not a member of this team." });
            }

            var messages = await _chatService.GetTeamMessagesAsync(teamId, page, pageSize);
            return Ok(messages);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var count = await _chatService.GetTotalUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromQuery] int? conversationId, [FromQuery] int? teamId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _chatService.MarkReadAsync(userId, conversationId, teamId);
            
            // Notify other tabs of same user to update UI
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ChatRead", new { conversationId, teamId });
            
            return Ok();
        }
    }
}
