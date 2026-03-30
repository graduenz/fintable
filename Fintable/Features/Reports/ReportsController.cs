using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Reports
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class ReportsController(IReportsService service) : ControllerBase
    {
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            return Ok(await service.GetStatsReportAsync());
        }

        [HttpGet("fintable")]
        public async Task<IActionResult> Fintable([FromQuery] int? year)
        {
            var resolvedYear = year ?? DateTime.UtcNow.Year;
            return Ok(await service.GetFintableReportAsync(resolvedYear));
        }

        [HttpGet("fintable/pdf")]
        public async Task<IActionResult> FintablePdf([FromQuery] int? year)
        {
            var resolvedYear = year ?? DateTime.UtcNow.Year;
            var report = await service.GetFintableReportAsync(resolvedYear);
            var pdf = FintableReportPdfGenerator.Generate(report);
            return File(pdf, "application/pdf", $"fintable-{resolvedYear}.pdf");
        }
    }
}
