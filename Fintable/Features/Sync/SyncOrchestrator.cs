using Fintable.Organizze;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Fintable.Features.Sync
{
    public class SyncOrchestrator(
        FintableDb db,
        IOptions<SyncWindowOptions> syncWindowOptions,
        ILogger<SyncOrchestrator> logger,
        ILogger<OrganizzeSyncService> organizzeSyncLogger) : ISyncOrchestrator
    {
        private readonly SyncWindowOptions _syncWindowOptions = syncWindowOptions.Value;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var providers = await db.Providers.ToListAsync(cancellationToken);
            logger.LogInformation("Starting sync for {ProviderCount} provider(s).", providers.Count);

            foreach (var provider in providers)
            {
                await SyncProviderAsync(provider, cancellationToken);
            }

            logger.LogInformation("Finished sync for {ProviderCount} provider(s).", providers.Count);
        }

        public async Task<bool> ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default)
        {
            var provider = await db.Providers.FindAsync([providerId], cancellationToken);
            if (provider is null)
            {
                logger.LogWarning("Provider {ProviderId} was not found for sync.", providerId);
                return false;
            }

            await SyncProviderAsync(provider, cancellationToken);
            return true;
        }

        private async Task SyncProviderAsync(Provider provider, CancellationToken cancellationToken)
        {
            // For now we only support Organizze; this can expand as new providers are added.
            if (!string.Equals(provider.Type.Trim(), ProviderType.Organizze, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Skipping provider {ProviderId} ({ProviderName}) because type {ProviderType} is not supported.",
                    provider.Id,
                    provider.Name,
                    provider.Type);
                return;
            }

            var metadataJson = JsonSerializer.Serialize(provider.Metadata ?? new Dictionary<string, string>());
            var metadata = OrganizzeMetadata.FromJson(metadataJson);

            var client = new NOrganizze.NOrganizzeClient(metadata.ToCredentials);
            var syncService = new OrganizzeSyncService(db, client, _syncWindowOptions, organizzeSyncLogger);

            await syncService.SyncAsync(provider, cancellationToken);
        }
    }
}
