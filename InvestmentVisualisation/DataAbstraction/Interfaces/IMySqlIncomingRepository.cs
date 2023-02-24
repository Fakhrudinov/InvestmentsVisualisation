using DataAbstraction.Models.Incoming;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlIncomingRepository
    {
        Task<string> CreateNewIncoming(CreateIncomingModel newIncoming);
        Task<string> DeleteSingleIncoming(int id);
        Task<string> EditSingleIncoming(IncomingModel newIncoming);
        Task<int> GetIncomingCount();
        Task<List<IncomingModel>> GetPageFromIncoming(int itemsAtPage, int pageNumber);
        Task<string> GetSecCodeFromLastRecord();
        Task<IncomingModel> GetSingleIncomingById(int id);
    }
}
