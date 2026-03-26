namespace Fintable.Features.Sync
{
    public interface ISyncOrchestrator
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);

        Task<bool> ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
