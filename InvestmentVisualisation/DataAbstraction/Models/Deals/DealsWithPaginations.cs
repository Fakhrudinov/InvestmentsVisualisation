namespace DataAbstraction.Models.Deals
{
    public class DealsWithPaginations
    {
        public List<DealModel> Deals { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
    }
}
