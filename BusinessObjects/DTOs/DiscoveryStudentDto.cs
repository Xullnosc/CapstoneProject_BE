using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public record DiscoveryStudentDto(
    int UserId,
    string FullName,
    string? StudentCode,
    string? Avatar,
    string? MajorName,
    List<UserSkillDto> Skills
);
