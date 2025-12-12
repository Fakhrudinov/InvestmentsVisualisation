namespace DataAbstraction.Models.Deals
{
	public class LatestDealsModel
	{
		public string SecCode { get; set; }
		public DateOnly DealDate { get; set; }
		public decimal AvPrice { get; set; }
		public int Pieces { get; set; }
	}
}
