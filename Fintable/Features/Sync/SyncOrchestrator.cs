using Fintable.Persistence;
using Fintable.Features.Providers.Organizze;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Fintable.Features.Sync
{
    public class SyncOrchestrator(FintableDb db) : ISyncOrchestrator
    {
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var providers = await db.Providers.ToListAsync(cancellationToken);

            foreach (var provider in providers)
            {
                await SyncProviderAsync(provider, cancellationToken);
            }
        }

        public async Task ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default)
        {
            var provider = await db.Providers.FindAsync([providerId], cancellationToken);
            if (provider is null)
            {
                return;
            }

            await SyncProviderAsync(provider, cancellationToken);
        }

        private async Task SyncProviderAsync(Provider provider, CancellationToken cancellationToken)
        {
            // For now we only support Organizze; this can expand as new providers are added.
            if (!string.Equals(provider.Name.Trim(), "organizze", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var metadataJson = JsonSerializer.Serialize(provider.Metadata ?? new Dictionary<string, string>());
            var metadata = OrganizzeMetadata.FromJson(metadataJson);

            var fetchClient = new OrganizzeFetchClient(metadata);
            var syncService = new OrganizzeSyncService(db, fetchClient);

            await syncService.SyncAsync(provider, cancellationToken);
        }
    }
}
