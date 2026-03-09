namespace Fintable.Features.Sync
{
    public interface ISyncOrchestrator
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);

        Task ExecuteForProviderAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
