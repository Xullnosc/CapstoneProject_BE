using Microsoft.AspNetCore.Mvc;
using capstone_be.Models;
using Microsoft.EntityFrameworkCore;
namespace capstone_be.Controllers
{
    [ApiController] // Marks this class as an API controller
    [Route("api/[controller]")] // Sets the base route: api/MyExample
    public class TestController : ControllerBase
    {
        private CapstoneDbContext _capstoneDbContext;
        // GET api/MyExample

        public TestController(CapstoneDbContext capstoneDbContext)
        {
            _capstoneDbContext = capstoneDbContext;
        }
        [HttpGet]
        
        public async Task<IActionResult> Get()
        {
            var history = await _capstoneDbContext.FlywaySchemaHistories.ToListAsync();
            return Ok(history);
        }

    }
}