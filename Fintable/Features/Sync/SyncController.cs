using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Sync
{
    [ApiController]
    [Route("[controller]")]
    public class SyncController(ISyncOrchestrator orchestrator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SyncAll(CancellationToken cancellationToken)
        {
            await orchestrator.ExecuteAsync(cancellationToken);
            return Ok();
        }

        [HttpPost("{providerId}")]
        public async Task<IActionResult> SyncProvider([FromRoute] string providerId, CancellationToken cancellationToken)
        {
            await orchestrator.ExecuteForProviderAsync(providerId, cancellationToken);
            return Ok();
        }
    }
}
