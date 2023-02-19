using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
    public interface IDataBaseRepository
    {
        Task<string> CreateNewIncoming(CreateIncomingModel newIncoming);
        Task<string> DeleteSingleIncoming(int id);
        Task<string> EditSingleIncoming(IncomingModel newIncoming);
        Task<int> GetIncomingCount();
        Task<List<IncomingModel>> GetPageFromIncoming(int itemsAtPage, int pageNumber);
        Task<IncomingModel> GetSingleIncomingById(int id);
    }
}
