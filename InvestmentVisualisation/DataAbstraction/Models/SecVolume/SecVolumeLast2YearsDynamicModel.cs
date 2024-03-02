using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.SecVolume
{
    public class SecVolumeLast2YearsDynamicModel
    {
        [Display(Name = "Тикер")]
        public string SecCode { get; set; }
        [Display(Name = "Имя")]
        public string Name { get; set; }

        public int ? PreviousPreviousYearPieces { get; set; }

        public int ? PreviousYearPieces { get; set; }

        public decimal ? LastYearChanges { get; set; }
        public int ? LastYearPieces { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.00}")]
        public decimal ? LastYearVolume { get; set; }

        public string ? SmartLabDividents { get; set; }
        public string? DohodDividents { get; set; }
        public string? VsdelkeDividents { get; set; }

        public string LineColor { get; set; } = "white; color: black";

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        public DateTime NextDivDate { get; set; }
    }
}
