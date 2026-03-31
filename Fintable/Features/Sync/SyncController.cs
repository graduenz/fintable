using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Fintable.Features.Sync
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class SyncController(ISyncOrchestrator orchestrator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SyncAll(CancellationToken cancellationToken)
        {
            var result = await orchestrator.ExecuteAsync(cancellationToken);
            return Ok(result);
        }

        [HttpPost("{providerId}")]
        public async Task<IActionResult> SyncProvider([FromRoute] string providerId, CancellationToken cancellationToken)
        {
            var result = await orchestrator.ExecuteForProviderAsync(providerId, cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
    }
}
