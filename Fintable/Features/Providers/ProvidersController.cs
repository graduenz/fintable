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
            var type = providerDto.Type?.Trim() ?? "";
            if (ProviderMetadataSchemaRegistry.GetRequiredKeys(type) is null)
                return BadRequest("Unknown provider type.");

            var provider = providerDto.Adapt<Provider>();
            provider.Id = Id.New();
            provider.Type = type;

            db.Providers.Add(provider);
            await db.SaveChangesAsync();

            var createdDto = provider.Adapt<ProviderDto>();
            return CreatedAtAction(nameof(Get), new { id = provider.Id }, createdDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] ProviderDto providerDto)
        {
            var type = providerDto.Type?.Trim() ?? "";
            if (ProviderMetadataSchemaRegistry.GetRequiredKeys(type) is null)
                return BadRequest("Unknown provider type.");

            var provider = await db.Providers.FindAsync(id);
            if (provider == null)
                return NotFound();

            provider.Type = type;
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

        [HttpGet("{type}/validate")]
        public async Task<IActionResult> Validate([FromRoute] string type)
        {
            var requiredKeys = ProviderMetadataSchemaRegistry.GetRequiredKeys(type);
            if (requiredKeys is null)
                return NotFound();

            var normalizedType = type.Trim();
            var providers = await db.Providers
                .Where(p => string.Equals(p.Type.Trim(), normalizedType, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            var providersDict = new Dictionary<string, ProviderValidateEntryDto>();
            var allFullySetUp = true;

            foreach (var p in providers)
            {
                var missing = requiredKeys
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
