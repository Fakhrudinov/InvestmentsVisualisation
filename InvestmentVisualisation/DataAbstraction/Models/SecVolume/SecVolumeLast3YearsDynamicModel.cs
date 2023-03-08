using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.SecVolume
{
    public class SecVolumeLast3YearsDynamicModel
    {
        [Display(Name = "Тикер")]
        public string SecCode { get; set; }
        [Display(Name = "Имя")]
        public string Name { get; set; }

        public int ? PreviousPreviousYearPieces { get; set; }

        public decimal ? PreviousYearChanges { get; set; }
        public int ? PreviousYearPieces { get; set; }

        public decimal LastYearChanges { get; set; }
        public int LastYearPieces { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.000000}")]
        public decimal LastYearVolume { get; set; }

        public string SmartLabDividents { get; set; }
    }
}
