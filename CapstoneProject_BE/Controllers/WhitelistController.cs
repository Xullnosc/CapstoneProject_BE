using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CapstoneProject_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WhitelistController : ControllerBase
    {
        private readonly IWhitelistService _whitelistService;

        public WhitelistController(IWhitelistService whitelistService)
        {
            _whitelistService = whitelistService;
        }

        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<Whitelist>>> GetWhitelistByRole(int roleId)
        {
            var result = await _whitelistService.GetWhitelistByRoleAsync(roleId);
            return Ok(result);
        }
    }
}
