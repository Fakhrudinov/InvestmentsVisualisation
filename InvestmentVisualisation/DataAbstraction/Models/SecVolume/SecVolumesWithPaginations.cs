namespace DataAbstraction.Models.SecVolume
{
    public class SecVolumesWithPaginations
    {
        public List<SecVolumeModel> SecVolumes { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
        public int ShownYear { get; set; }
    }
}
