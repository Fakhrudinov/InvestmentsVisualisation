using DataAbstraction.Models.BankDeposits;
using DataAbstraction.Models.MoneyByMonth;

namespace DataAbstraction.Interfaces
{
	public interface IMySqlBankDepositsRepository
	{
		Task<string> Close(CloseBankDepositModel closeBankDeposit, CancellationToken cancellationToken);
		Task<string> Create(NewBankDepositModel model, CancellationToken cancellationToken);
		Task<string> Edit(BankDepositModel model, CancellationToken cancellationToken);
		Task<int> GetActiveBankDepositsCount(CancellationToken cancellationToken);
		Task<int> GetAnyBankDepositsCount(CancellationToken cancellationToken);
		Task<List<BankDepoDBModel>?> GetBankDepoChartData(CancellationToken cancellationToken);
		Task<BankDepositModel?> GetBankDepositById(int id, CancellationToken cancellationToken);
		Task<List<BankDepoDBPaymentData>?> GetBankDepositsEndedAfterDate(CancellationToken cancellationToken, string v);
		Task<List<BankDepositModel> ?> GetPageWithActiveBankDeposits(
			int itemsAtPage, 
			int pageNumber,
			CancellationToken cancellationToken);
		Task<List<BankDepositModel>?> GetPageWithAnyBankDeposits(
			int itemsAtPage, 
			int pageNumber,
			CancellationToken cancellationToken);
	}
}
