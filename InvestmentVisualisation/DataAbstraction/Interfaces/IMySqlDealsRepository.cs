using DataAbstraction.Models.Deals;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlDealsRepository
    {
        Task<string> CreateNewDeal(CreateDealsModel model);
        Task<string> DeleteSingleDeal(int id);
        Task<string> EditSingleDeal(DealModel model);
        Task<int> GetDealsCount();
        Task<int> GetDealsSpecificSecCodeCount(string secCode);
        Task<List<DealModel>> GetPageFromDeals(int itemsAtPage, int v);
        Task<List<DealModel>> GetPageFromDealsSpecificSecCode(string secCode, int itemsAtPage, int v);
        Task<DealModel> GetSingleDealById(int id);
    }
}
