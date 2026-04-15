using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public record DiscoveryTeamDto(
    int TeamId,
    string TeamName,
    string? TeamAvatar,
    int LeaderId,         
    string? Description,
    int CurrentMemberCount,
    int MaxMembers,       
    List<string> Skills,
    string? TeamCode,
    bool HasPendingJoinRequest = false,
    bool IsUserInTeam = false
);
