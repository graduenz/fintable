using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Sync
{
    [ApiController]
    [Route("[controller]")]
    public class SyncController(ISyncOrchestrator orchestrator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Sync()
        {
            throw new NotImplementedException();
        }
    }
}
