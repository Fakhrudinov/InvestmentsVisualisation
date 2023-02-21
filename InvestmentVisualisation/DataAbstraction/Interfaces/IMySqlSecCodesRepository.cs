using DataAbstraction.Models.SecCodes;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecCodesRepository
    {
        Task<string> CreateNewSecCode(SecCodeInfo model);
        Task<string> EditSingleSecCode(SecCodeInfo model);
        Task<List<SecCodeInfo>> GetPageFromSecCodes(int itemsAtPage, int v);
        Task<int> GetSecCodesCount();
        Task<SecCodeInfo> GetSingleSecCodeBySecCode(string secCode);
    }
}
