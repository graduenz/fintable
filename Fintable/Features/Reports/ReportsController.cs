using Fintable.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Reports
{
    [ApiController]
    [Route("[controller]")]
    public class ReportsController(IReportsService service) : ControllerBase
    {
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            return Ok(await service.GetStatsReportAsync());
        }

        [HttpGet("fintable")]
        public async Task<IActionResult> Fintable()
        {
            throw new NotImplementedException();
        }
    }
}
