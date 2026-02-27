using Fintable.Persistence;

namespace Fintable.DataSource
{
    public class OrganizzeDataSource : IDataSource
    {
        public Task<IReadOnlyList<Account>> GetAccountsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Category>> GetCategoriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<CreditCard>> GetCreditCardsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Invoice>> GetInvoicesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Transaction>> GetTransactionsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
