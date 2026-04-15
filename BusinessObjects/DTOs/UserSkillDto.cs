using System.Collections.Generic;

namespace BusinessObjects.DTOs;

public record UserSkillDto(int SkillId, string SkillTag, string SkillLevel);

public record UpdateUserSkillsRequest(List<SkillEntry> Skills);

public record SkillEntry(string SkillTag, string SkillLevel);
