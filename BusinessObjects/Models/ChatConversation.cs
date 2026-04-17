using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class ChatConversation
{
    public int ConversationId { get; set; }
    public int User1Id { get; set; }  // Always the smaller UserId
    public int User2Id { get; set; }  // Always the larger UserId
    public int SemesterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User User1 { get; set; } = null!;
    public virtual User User2 { get; set; } = null!;
    public virtual Semester Semester { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
