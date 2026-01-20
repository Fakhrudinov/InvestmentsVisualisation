using DataAbstraction.Models.WishList;


namespace DataAbstraction.Models.SecVolume
{
	public class NextBuyModel : WishListItemModel
	{
		public int WishVolume { get; set; }
		public decimal RealVolume { get; set; }
		public decimal BuyVolume { get; set; }
		public string RecommendBuyVolume { get; set; }


		// for deals in last period
		public DateOnly ? LastDealDate { get; set; }
		public decimal ? LatestDealsVolume { get; set; }
	}
}
