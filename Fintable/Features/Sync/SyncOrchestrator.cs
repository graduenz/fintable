using Fintable.DataSource;
using Fintable.Persistence;

namespace Fintable.Features.Sync
{
    public class SyncOrchestrator(FintableDb db, IDataSourceFactory dataSourceFactory) : ISyncOrchestrator
    {
        public async Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
