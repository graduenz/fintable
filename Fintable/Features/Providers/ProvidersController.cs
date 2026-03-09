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

            provider.Type = providerDto.Type;
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

        [HttpGet("{name}/validate")]
        public async Task<IActionResult> Validate([FromRoute] string name)
        {
            var providers = await db.Providers.Where(p => p.Name == name).ToListAsync();
            if (providers.Count == 0)
                return NotFound();

            var first = providers[0];
            var requiredKeys = ProviderMetadataSchemaRegistry.GetRequiredKeys(first.Type) ?? [];

            var providersDict = new Dictionary<string, ProviderValidateEntryDto>();
            var allFullySetUp = true;

            foreach (var p in providers)
            {
                var keysForType = ProviderMetadataSchemaRegistry.GetRequiredKeys(p.Type) ?? [];
                var missing = keysForType
                    .Where(k =>
                    {
                        var value = p.Metadata?.FirstOrDefault(m => string.Equals(m.Key, k, StringComparison.OrdinalIgnoreCase)).Value;
                        return string.IsNullOrWhiteSpace(value);
                    })
                    .ToList();
                var isFullySetUp = missing.Count == 0;
                if (!isFullySetUp)
                    allFullySetUp = false;

                providersDict[p.Id] = new ProviderValidateEntryDto
                {
                    Id = p.Id,
                    Type = p.Type,
                    Name = p.Name,
                    IsFullySetUp = isFullySetUp,
                    MissingKeys = missing,
                };
            }

            var result = new ProviderValidateResultDto
            {
                RequiredKeys = requiredKeys,
                IsFullySetUp = allFullySetUp,
                Providers = providersDict,
            };
            return Ok(result);
        }
    }
}
