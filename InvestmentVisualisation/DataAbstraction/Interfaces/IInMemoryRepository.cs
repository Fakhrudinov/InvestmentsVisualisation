using DataAbstraction.Models.Deals;
using DataAbstraction.Models.Incoming;

namespace DataAbstraction.Interfaces
{
    public interface IInMemoryRepository
    {
        void AddNewDeal(CreateDealsModel newDeal);
		void AddNewIncoming(CreateIncomingModel newIncoming);
		void CombineTwoDealsById(Guid targetDeal, Guid sourceDeal);

        void DeleteAllDeals();
		void DeleteAllIncomings();

		void DeleteSingleDealByStringId(string id);
		void DeleteSingleIncomingByStringId(string id);

		void EditSingleDeal(IndexedDealModel editDeal);
		void EditSingleIncoming(IndexedIncomingModel model);

		List<IndexedDealModel> GetAllDeals();
		List<IndexedIncomingModel>? GetAllIncomings();

		CreateDealsModel? GetCreateDealsModelByGuid(Guid id);
        CreateDealsModel? GetCreateDealsModelById(string id);
		CreateIncomingModel? GetCreateIncomingModelByGuid(Guid id);
		CreateIncomingModel? GetCreateIncomingModelById(string id);

		int ReturnDealsCount();
		int ReturnIncomingsCount();
	}
}
