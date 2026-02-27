using Fintable.Persistence;

namespace Fintable.DataSource
{
    public class DataSourceFactory : IDataSourceFactory
    {
        public IDataSource CreateForProvider(Provider provider)
        {
            return provider.Name.Trim().ToLowerInvariant() switch
            {
                "organizze" => new OrganizzeDataSource(),
                _ => throw new NotSupportedException($"Provider '{provider.Name}' is not supported.")
            };
        }
    }
}
