namespace DataAbstraction.Models.MoneyByMonth
{
	public class BankDepoDBBaseModel
	{
		public string Name { get; set; }
		public DateTimeOffset DateClose { get; set; }
		public decimal Percent { get; set; }
		public decimal SummAmount { get; set; }
	}
}
