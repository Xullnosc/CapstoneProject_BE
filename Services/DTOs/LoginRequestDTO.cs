using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTOs
{
    public class LoginRequestDTO
    {
        public string IdToken { get; set; } = null!;
        public string Campus { get; set; } = null!;
    }
}
