using Fintable.Persistence;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Features.Providers
{
    [ApiController]
    [Route("[controller]")]
    public class ProvidersController(FintableDb db) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] string id)
        {
            var provider = await db.Providers.FindAsync(id);
            if (provider == null)
                return NotFound();

            var dto = provider.Adapt<ProviderDto>();
            return Ok(dto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var providers = await db.Providers.ToListAsync();
            var dtos = providers.Adapt<List<ProviderDto>>();
            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProviderDto providerDto)
        {
            var provider = providerDto.Adapt<Provider>();
            provider.Id = Id.New();

            db.Providers.Add(provider);
            await db.SaveChangesAsync();

            var createdDto = provider.Adapt<ProviderDto>();
            return CreatedAtAction(nameof(Get), new { id = provider.Id }, createdDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] ProviderDto providerDto)
        {
            var provider = await db.Providers.FindAsync(id);
            if (provider == null)
                return NotFound();

            provider.Name = providerDto.Name;
            provider.Metadata = providerDto.Metadata;
            await db.SaveChangesAsync();

            var updatedDto = provider.Adapt<ProviderDto>();
            return Ok(updatedDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var provider = await db.Providers.FindAsync(id);
            if (provider == null)
                return NotFound();

            db.Providers.Remove(provider);
            await db.SaveChangesAsync();

            return NoContent();
        }
    }
}
