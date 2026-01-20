
using DataAbstraction.Models.ExtraordinaryBuy;

namespace DataAbstraction.Interfaces
{
	public interface IMySqlExtraordinaryBuyRepository
	{
		Task<string> CreateNew(CancellationToken cancellationToken, string seccode, int volume, string ? description);
		Task<string> Delete(CancellationToken cancellationToken, string seccode);
		Task<string> Edit(CancellationToken cancellationToken, string seccode, int volume, string ? description);
		Task<int> GetExtraordinaryBuyCount(CancellationToken cancellationToken);
		Task<List<ExtraordinaryBuyModel>?> GetPageFromExtraordinaryBuy(CancellationToken cancellationToken, int itemsAtPage, int v);
	}
}
