using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models
{
    public class CreateIncomingModel
    {
        public string SecCode { get; set; }
        public DateTime Date { get; set; }
        [Display(Name = "Category")]
        public int Category { get; set; }

        [Display(Name = "Сумма")]
        public decimal Value { get; set; }

        [Display(Name = "Комиссия")]
        public decimal? Comission { get; set; }
    }
}

