using DataAbstraction.Models.Incoming;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlIncomingRepository
    {
        Task<string> CreateNewIncoming(CancellationToken cancellationToken, CreateIncomingModel newIncoming);
        Task<string> DeleteSingleIncoming(CancellationToken cancellationToken, int id);
        Task<string> EditSingleIncoming(CancellationToken cancellationToken, IncomingModel newIncoming);
        Task<int> GetIncomingCount();
        Task<int> GetIncomingSpecificSecCodeCount(string secCode);
        Task<List<IncomingModel>> GetPageFromIncoming(CancellationToken cancellationToken, int itemsAtPage, int pageNumber);
        Task<List<IncomingModel>> GetPageFromIncomingSpecificSecCode(CancellationToken cancellationToken, string secCode, int itemsAtPage, int v);
        Task<string> GetSecCodeFromLastRecord(CancellationToken cancellationToken);
        Task<IncomingModel> GetSingleIncomingById(CancellationToken cancellationToken, int id);
    }
}
