using DataAbstraction.Models.SecVolume;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecVolumeRepository
    {
        Task<int> GetSecVolumeCountForYear(int year);
        Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamic(int year);
        Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamicSortedByVolume(int year);
        Task<List<SecVolumeModel>> GetSecVolumePageForYear(int itemsAtPage, int v, int year);
		Task<List<ChartItemModel>> GetVolumeChartData();
		Task RecalculateSecVolumeForYear(int year);
    }
}
