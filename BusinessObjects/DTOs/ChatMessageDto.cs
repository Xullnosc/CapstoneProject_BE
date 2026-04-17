using System;

namespace BusinessObjects.DTOs;

public record ChatMessageDto(
    int MessageId,
    int SenderId,
    string SenderName,
    string? SenderAvatar,
    string Content,
    string MessageType,
    string? AttachmentUrl,
    string? AttachmentName,
    DateTime CreatedAt,
    int? ConversationId = null,
    int? TeamId = null
);

public record SendMessageRequest(
    string Content,
    string MessageType = "text",
    string? AttachmentUrl = null,
    string? AttachmentName = null
);
