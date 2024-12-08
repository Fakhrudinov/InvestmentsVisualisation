namespace DataAbstraction.Models.MoneyByMonth
{
	public class BankDepoDBPaymentData : BankDepoDBBaseModel
	{
		public decimal ? IncomeSummAmount { get; set; }
		public bool IsOpen { get; set; } = false;
		public int DaysOfDeposit { get; set; }
	}

	//// bd.name,
	//// bd.date_close,
	//// bd.summ,
	//// bd.percent,
	//
	// bd.isopen,
	// bd.income_summ,
	// DATEDIFF(bd.date_close,bd.date_open) as days
}
