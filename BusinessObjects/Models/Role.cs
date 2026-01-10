using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Whitelist> Whitelists { get; set; } = new List<Whitelist>();
}
