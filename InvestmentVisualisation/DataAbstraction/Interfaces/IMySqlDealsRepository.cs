using DataAbstraction.Models.Deals;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlDealsRepository
    {
        Task<int> GetDealsCount();
        Task<List<DealModel>> GetPageFromDeals(int itemsAtPage, int v);
    }
}
