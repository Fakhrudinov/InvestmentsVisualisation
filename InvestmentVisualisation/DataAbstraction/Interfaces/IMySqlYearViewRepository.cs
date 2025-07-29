
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.YearView;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlYearViewRepository
    {
        Task CallFillViewShowLast12Month(CancellationToken cancellationToken);
        Task DropTableLast12MonthView(CancellationToken cancellationToken);
		Task<List<SecCodeAndDividentAndDateModel>?> GetBondsDividendsForLastYear(CancellationToken cancellationToken);
		Task<List<NameAndPiecesAndValueModel>?> GetBondsWithNameAndValues(CancellationToken cancellationToken);
		Task<List<YearViewModel>> GetLast12MonthViewPage(CancellationToken cancellationToken);
        Task<List<YearViewModel>> GetYearViewPage(CancellationToken cancellationToken);
        Task RecalculateYearView(CancellationToken cancellationToken, int year, bool sortedByVolume);
    }
}
