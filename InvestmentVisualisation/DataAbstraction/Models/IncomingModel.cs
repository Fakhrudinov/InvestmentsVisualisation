using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models
{
    public class IncomingModel
    {

        public int Id { get; set; }

        [Display (Name = "Дата")]
        public DateTime Date { get; set; }

        [Display(Name = "Тикер")]
        public string SecCode { get; set; }

        [Display(Name = "SecBoard")]
        public int SecBoard { get; set; }

        [Display(Name = "Category")]
        public int Category { get; set; }

        [Display(Name = "Сумма")]
        public string Value { get; set; }

        [Display(Name = "Комиссия")]
        public string ? Comission { get; set; }
    }
}
