using Fintable.Persistence;

namespace Fintable.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource CreateForProvider(Provider provider);
    }
}
