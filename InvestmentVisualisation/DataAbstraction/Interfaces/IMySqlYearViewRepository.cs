
using DataAbstraction.Models.YearView;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlYearViewRepository
    {
        Task CallFillViewShowLast12Month();
        Task DropTableLast12MonthView();
        Task<List<YearViewModel>> GetLast12MonthViewPage();
        Task<List<YearViewModel>> GetYearViewPage();
        Task RecalculateYearView(int year, bool sortedByVolume);
    }
}
