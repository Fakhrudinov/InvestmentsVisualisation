using DataAbstraction.Models.SecCodes;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecCodesRepository
    {
        Task<string> CreateNewSecCode(CancellationToken cancellationToken, SecCodeInfo model);
        Task<string> EditSingleSecCode(CancellationToken cancellationToken, SecCodeInfo model);
        Task<List<SecCodeInfo>> GetPageFromSecCodes(CancellationToken cancellationToken, int itemsAtPage, int v);
        Task<string> GetSecCodeByISIN(CancellationToken cancellationToken, string isin);
        Task<int> GetSecCodesCount(CancellationToken cancellationToken);
        Task<SecCodeInfo> GetSingleSecCodeBySecCode(CancellationToken cancellationToken, string secCode);
        void RenewStaticSecCodesList(CancellationToken cancellationToken);
    }
}
