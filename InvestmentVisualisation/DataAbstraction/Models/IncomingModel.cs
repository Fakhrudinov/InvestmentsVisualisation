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

        [Display(Name = "Сумма"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string Value { get; set; }

        [Display(Name = "Комиссия"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string ? Comission { get; set; }
    }
}
