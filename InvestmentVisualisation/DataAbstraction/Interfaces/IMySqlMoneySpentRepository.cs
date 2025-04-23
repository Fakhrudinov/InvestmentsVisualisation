using DataAbstraction.Models.MoneySpent;

namespace DataAbstraction.Interfaces
{
	public interface IMySqlMoneySpentRepository
	{
		Task<string> CreateNewItem(CancellationToken cancellationToken, MoneySpentByMonthModel lastItem);
		Task<string> EditMoneySpentItem(CancellationToken cancellationToken, MoneySpentByMonthModel model);
		Task<MoneySpentByMonthModel?> GetLastRow(CancellationToken cancellationToken);
		Task<List<MoneySpentAndIncomeModel>?> GetMoneySpentAndIncomeModelChartData(CancellationToken cancellationToken);
		Task<int> GetMoneySpentCount(CancellationToken cancellationToken);
		Task<List<MoneySpentByMonthModel>?> GetPageFromMoneySpent(
			CancellationToken cancellationToken, 
			int itemsAtPage, 
			int pageNumber);
		Task<MoneySpentByMonthModel?> GetSingleRowByDateTime(CancellationToken cancellationToken, DateTime date);
	}
}
