using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
    public interface IDataBaseRepository
    {
        Task<int> GetIncomingCount();
        Task<List<IncomingModel>> GetPageFromIncoming(int itemsAtPage, int pageNumber);
    }
}
