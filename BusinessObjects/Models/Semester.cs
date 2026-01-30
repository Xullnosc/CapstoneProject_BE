using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Semester
{
    public int SemesterId { get; set; }

    public string SemesterCode { get; set; } = null!;

    public string SemesterName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ICollection<Whitelist> Whitelists { get; set; } = new List<Whitelist>();
}
