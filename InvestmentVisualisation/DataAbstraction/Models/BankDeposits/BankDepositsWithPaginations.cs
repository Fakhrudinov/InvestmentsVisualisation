namespace DataAbstraction.Models.BankDeposits
{
	public class BankDepositsWithPaginations
	{
		public List<BankDepositModel> ? BankDeposits { get; set; }
		public PaginationPageViewModel PageViewModel { get; set; } = new PaginationPageViewModel(0,0,0);
	}
}
