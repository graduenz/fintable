namespace Fintable.Features.Sync
{
    public interface ISyncOrchestrator
    {
        Task<SyncExecutionResultDto> ExecuteAsync(CancellationToken cancellationToken = default);

        Task<SyncExecutionResultDto?> ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
