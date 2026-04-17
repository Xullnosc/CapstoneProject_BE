using System;

namespace BusinessObjects.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }
    public int? ConversationId { get; set; }  // NULL = Team chat
    public int? TeamId { get; set; }          // NULL = DM chat
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    public string MessageType { get; set; } = "text"; // text|image|file
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public virtual ChatConversation? Conversation { get; set; }
    public virtual Team? Team { get; set; }
    public virtual User Sender { get; set; } = null!;
}
