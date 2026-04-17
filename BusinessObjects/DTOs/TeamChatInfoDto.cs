using System;

namespace BusinessObjects.DTOs;

public record TeamChatInfoDto(
    int TeamId,
    string TeamName,
    string? TeamAvatar,
    string? LastMessage,
    DateTime? LastMessageAt,
    int UnreadCount
);
