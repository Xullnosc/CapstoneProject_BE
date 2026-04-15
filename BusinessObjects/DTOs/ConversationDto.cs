using System;

namespace BusinessObjects.DTOs;

public record ConversationDto(
    int ConversationId,
    int OtherUserId,
    string OtherUserName,
    string? OtherUserAvatar,
    string? LastMessage,
    DateTime? LastMessageAt,
    int UnreadCount
);
