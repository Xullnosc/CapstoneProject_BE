using System;

namespace BusinessObjects.Models
{
    public partial class RegisteredEnterprise
    {
        public int Id { get; set; }
        public string EnterpriseName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
