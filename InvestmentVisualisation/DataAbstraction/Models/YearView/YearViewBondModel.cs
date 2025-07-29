namespace DataAbstraction.Models.YearView
{
	public class YearViewBondModel : YearViewModel
	{
		public int PaymentCountInYear { get; set; } = 12;
		public decimal ExpectedForYearDivSumm { get; set; } = 0;
		public decimal ExpectedForYearDivPercent { get; set; } = 0;
	}
}
