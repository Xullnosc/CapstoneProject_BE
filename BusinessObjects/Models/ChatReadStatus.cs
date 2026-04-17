using System;

namespace BusinessObjects.Models;

public partial class ChatReadStatus
{
    public int StatusId { get; set; }
    public int UserId { get; set; }
    public int? ConversationId { get; set; }
    public int? TeamId { get; set; }
    public DateTime LastReadAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ChatConversation? Conversation { get; set; }
    public virtual Team? Team { get; set; }
}
