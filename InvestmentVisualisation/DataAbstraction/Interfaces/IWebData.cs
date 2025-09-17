using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.MoneyByMonth;

namespace DataAbstraction.Interfaces
{
    public interface IWebData
    {
		Task<bool> CreateNewSecCode(CancellationToken cancellationToken, string isin);
		Task<DohodDivsAndDatesModel?> GetDividentsTableFromDohod(CancellationToken cancellationToken);
        Task<List<SecCodeAndDividentModel>?> GetDividentsTableFromSmartLab(CancellationToken cancellationToken);
		Task<List<SecCodeAndDividentModel>?> GetDividentsTableFromVsdelke(CancellationToken cancellationToken);
		Task<List<ExpectedDividentsFromWebModel>?> GetFutureDividentsTableFromDohod(CancellationToken cancellationToken);
	}
}
