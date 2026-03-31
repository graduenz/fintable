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

        public async Task<SyncExecutionResultDto> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var providers = await db.Providers.ToListAsync(cancellationToken);
            using var collector = new SyncWarningCollector(logger, "sync run");
            var result = new SyncExecutionResultDto();
            logger.LogInformation("Starting sync for {ProviderCount} provider(s).", providers.Count);

            if (providers.Count == 0)
            {
                collector.ReportWarning(
                    SyncWarningCodes.NoProvidersToSync,
                    "There were no providers to sync.");
            }

            foreach (var provider in providers)
            {
                await SyncProviderAsync(provider, collector, result.SyncedProviders, cancellationToken);
            }

            logger.LogInformation("Finished sync for {ProviderCount} provider(s).", providers.Count);
            result.WarningGroups = [.. collector.WarningGroups];
            return result;
        }

        public async Task<SyncExecutionResultDto?> ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default)
        {
            var provider = await db.Providers.FindAsync([providerId], cancellationToken);
            if (provider is null)
            {
                logger.LogWarning("[provider_not_found] Provider {ProviderId} was not found for sync.", providerId);
                return null;
            }

            using var collector = new SyncWarningCollector(logger, $"provider sync ({provider.Id})");
            var result = new SyncExecutionResultDto();
            await SyncProviderAsync(provider, collector, result.SyncedProviders, cancellationToken);
            result.WarningGroups = [.. collector.WarningGroups];
            return result;
        }

        private async Task SyncProviderAsync(
            Provider provider,
            SyncWarningCollector collector,
            List<SyncedProviderDto> syncedProviders,
            CancellationToken cancellationToken)
        {
            // For now we only support Organizze; this can expand as new providers are added.
            if (!string.Equals(provider.Type.Trim(), ProviderType.Organizze, StringComparison.OrdinalIgnoreCase))
            {
                collector.ReportWarning(
                    SyncWarningCodes.ProviderTypeNotSupportedSkipped,
                    $"Skipping provider {provider.Id} ({provider.Name}) because type {provider.Type} is not supported.");
                syncedProviders.Add(new SyncedProviderDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    Type = provider.Type,
                    Outcome = SyncProviderOutcome.Skipped,
                });
                logger.LogInformation(
                    "Provider {ProviderId} ({ProviderName}) skipped due to unsupported type.",
                    provider.Id,
                    provider.Name);
                return;
            }

            var metadataJson = JsonSerializer.Serialize(provider.Metadata ?? new Dictionary<string, string>());
            var metadata = OrganizzeMetadata.FromJson(metadataJson);

            var client = new NOrganizze.NOrganizzeClient(metadata.ToCredentials);
            var syncService = new OrganizzeSyncService(db, client, _syncWindowOptions, organizzeSyncLogger);

            await syncService.SyncAsync(provider, collector, cancellationToken);
            syncedProviders.Add(new SyncedProviderDto
            {
                Id = provider.Id,
                Name = provider.Name,
                Type = provider.Type,
                Outcome = SyncProviderOutcome.Synced,
            });
        }
    }
}
