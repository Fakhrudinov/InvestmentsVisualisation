using DataAbstraction.Models.Deals;

namespace DataAbstraction.Interfaces
{
    public interface IInMemoryRepository
    {
        void AddNewDeal(CreateDealsModel newDeal);
        void CombineTwoDealsById(Guid targetDeal, Guid sourceDeal);
        void DeleteAllDeals();
        void DeleteSingleDealByStringId(string id);
        void EditSingleDeal(IndexedDealModel editDeal);
        List<IndexedDealModel> GetAllDeals();
        CreateDealsModel? GetCreateDealsModelByGuid(Guid id);
        CreateDealsModel? GetCreateDealsModelById(string id);
        int ReturnDealsCount();
    }
}
