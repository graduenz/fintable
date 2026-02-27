using Fintable.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Reports
{
    [ApiController]
    [Route("[controller]")]
    public class ReportsController(FintableDb db) : ControllerBase
    {
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            throw new NotImplementedException();
        }

        [HttpGet("fintable")]
        public async Task<IActionResult> Fintable()
        {
            throw new NotImplementedException();
        }
    }
}
