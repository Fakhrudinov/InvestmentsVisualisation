using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace DataAbstraction.Models.YearView
{
    public class YearViewModel
    {
        public string SecCode { get; set; }
        public string Name { get; set; }
        public string ? ISIN { get; set; }
        public int? Pieces { get; set; }

        public decimal? AvPrice { get; set; }

        [Display(Name = "Размер позиции")]
        public decimal Volume { get; set; }

        public decimal? Jan { get; set; }
        public decimal? Feb { get; set; }
        public decimal? Mar { get; set; }
        public decimal? Apr { get; set; }

        public decimal? May { get; set; }
        public decimal? Jun { get; set; }
        public decimal? Jul { get; set; }
        public decimal? Aug { get; set; }

        public decimal? Sep { get; set; }
        public decimal? Okt { get; set; }
        public decimal? Nov { get; set; }
        public decimal? Dec { get; set; }

        [Display(Name = "Сумма дивидентов")]
        public decimal? Summ { get; set; }

        [Display(Name = "% докупки")]
        public decimal? VolPercent { get; set; }

        [Display(Name = "% дивидент")]
        public decimal? DivPercent { get; set; }

        public string FullName { get; set; }
    }
}
