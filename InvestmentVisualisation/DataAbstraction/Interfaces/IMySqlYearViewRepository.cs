
using DataAbstraction.Models.YearView;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlYearViewRepository
    {
        Task<List<YearViewModel>> GetYearViewPage();
        Task RecalculateYearView(int year, bool sortedByVolume);
    }
}
