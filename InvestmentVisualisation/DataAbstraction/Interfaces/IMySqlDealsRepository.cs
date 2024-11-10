using DataAbstraction.Models.Deals;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlDealsRepository
    {
        Task<string> CreateNewDeal(CancellationToken cancellationToken, CreateDealsModel model);
        Task<string> CreateNewDealsFromList(CancellationToken cancellationToken, List<IndexedDealModel> model);
        Task<string> DeleteSingleDeal(CancellationToken cancellationToken, int id);
        Task<string> EditSingleDeal(CancellationToken cancellationToken, DealModel model);
        Task<int> GetDealsCount(CancellationToken cancellationToken);
        Task<int> GetDealsSpecificSecCodeCount(CancellationToken cancellationToken, string secCode);
        Task<List<DealModel>> GetPageFromDeals(CancellationToken cancellationToken, int itemsAtPage, int v);
        Task<List<DealModel>> GetPageFromDealsSpecificSecCode(CancellationToken cancellationToken, string secCode, int itemsAtPage, int v);
        Task<DealModel> GetSingleDealById(CancellationToken cancellationToken, int id);
    }
}
