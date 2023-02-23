using DataAbstraction.Models.BaseModels;
using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.SecVolume
{
    public class SecVolumeModel : SecCodeAndSecBoardModel
    {
        public int Pieces { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.000000}")]
        public decimal AvPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.000000}")]
        public decimal Volume { get; set; }
    }
}
