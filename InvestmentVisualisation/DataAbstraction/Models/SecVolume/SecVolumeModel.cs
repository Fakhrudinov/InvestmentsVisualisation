using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Models.SecVolume
{
    public class SecVolumeModel : SecCodeAndSecBoardModel
    {
        public int Pieces { get; set; }
        public decimal AvPrice { get; set; }
        public decimal Volume { get; set; }
    }
}
