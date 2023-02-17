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

        [Display(Name = "Тип")]
        public SecBoardCategory SecBoard { get; set; }

        [Display(Name = "Назначение")]
        public IncomingMoneyCategory Category { get; set; }

        [Display(Name = "Сумма")]
        public decimal Value { get; set; }

        [Display(Name = "Комиссия")]
        public decimal ? Comission { get; set; }
    }
}
