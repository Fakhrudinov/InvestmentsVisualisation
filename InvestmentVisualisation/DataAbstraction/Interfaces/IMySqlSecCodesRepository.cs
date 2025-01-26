using DataAbstraction.Models.SecCodes;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecCodesRepository
    {
        Task<string> CreateNewSecCode(CancellationToken cancellationToken, SecCodeInfo model);
        Task<string> EditSingleSecCode(CancellationToken cancellationToken, SecCodeInfo model);
		Task<int> GetOnlyActiveSecCodesCount(CancellationToken cancellationToken);
		Task<List<SecCodeInfo>> GetPageFromOnlyActiveSecCodes(CancellationToken cancellationToken, int itemsAtPage, int offset);
		Task<List<SecCodeInfo>> GetPageFromSecCodes(CancellationToken cancellationToken, int itemsAtPage, int offset);
        Task<string> GetSecCodeByISIN(CancellationToken cancellationToken, string isin);
        Task<int> GetSecCodesCount(CancellationToken cancellationToken);
        Task<SecCodeInfo> GetSingleSecCodeBySecCode(CancellationToken cancellationToken, string secCode);
        void RenewStaticSecCodesList(CancellationToken cancellationToken);
    }
}
