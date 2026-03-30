using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Campus
{
    public int CampusId { get; set; }

    public string CampusCode { get; set; } = null!;

    public string CampusName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();

    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();
    
    public virtual ICollection<Whitelist> Whitelists { get; set; } = new List<Whitelist>();
}
