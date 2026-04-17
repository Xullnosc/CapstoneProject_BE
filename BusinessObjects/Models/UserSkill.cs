using System;

namespace BusinessObjects.Models;

public partial class UserSkill
{
    public int SkillId { get; set; }
    public int UserId { get; set; }
    public string SkillTag { get; set; } = null!;
    public string SkillLevel { get; set; } = "Beginner";
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
