using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.Incoming
{
    public class CreateIncomingModel
    {
        public string SecCode { get; set; }

        public int SecBoard { get; set; }

        public DateTime Date { get; set; }
        [Display(Name = "Category")]
        public int Category { get; set; }


        [Display(Name = "Сумма"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string Value { get; set; }


        [Display(Name = "Комиссия"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string? Comission { get; set; }
    }
}

