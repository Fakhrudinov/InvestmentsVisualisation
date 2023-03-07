using DataAbstraction.Models.SecVolume;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlSecVolumeRepository
    {
        Task<int> GetSecVolumeCountForYear(int year);
        Task<List<SecVolumeLast3YearsDynamicModel>> GetSecVolumeLast3YearsDynamic(int year);
        Task<List<SecVolumeModel>> GetSecVolumePageForYear(int itemsAtPage, int v, int year);
        Task RecalculateSecVolumeForYear(int year);
    }
}
