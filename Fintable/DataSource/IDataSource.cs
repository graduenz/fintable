using Fintable.Persistence;

namespace Fintable.DataSource;

public interface IDataSource
{
    Task<IReadOnlyList<Account>> GetAccountsAsync();
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<IReadOnlyList<CreditCard>> GetCreditCardsAsync();
    Task<IReadOnlyList<Invoice>> GetInvoicesAsync();
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync();
}
