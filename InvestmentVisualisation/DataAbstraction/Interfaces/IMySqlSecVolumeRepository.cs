using DataAbstraction.Models.SecVolume;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecVolumeRepository
    {
        Task<int> GetSecVolumeCountForYear(CancellationToken cancellationToken, int year);
        Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamic(CancellationToken cancellationToken, int year);
        Task<List<SecVolumeLast2YearsDynamicModel>> GetSecVolumeLast3YearsDynamicSortedByVolume(CancellationToken cancellationToken, int year);
        Task<List<SecVolumeModel>> GetSecVolumePageForYear(CancellationToken cancellationToken, int itemsAtPage, int v, int year);
		Task<List<ChartItemModel>> GetVolumeChartData(CancellationToken cancellationToken);
		Task RecalculateSecVolumeForYear(CancellationToken cancellationToken, int year);
    }
}
