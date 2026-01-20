namespace DataAbstraction.Models.ExtraordinaryBuy
{
	public class ExtraordinaryBuyListWithPaginations
	{
		public List<ExtraordinaryBuyModel> ? ExtraordinaryBuy { get; set; }
		public PaginationPageViewModel PageViewModel { get; set; }
	}
}
