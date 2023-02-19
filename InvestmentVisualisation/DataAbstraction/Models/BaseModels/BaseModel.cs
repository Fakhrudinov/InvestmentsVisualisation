using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.BaseModels
{
    public class BaseModel
    {
        [Display(Name = "Дата")]
        public DateTime Date { get; set; }

        [Display(Name = "Тикер")]
        public string SecCode { get; set; }

        [Display(Name = "SecBoard")]
        public int SecBoard { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        [Display(Name = "Комиссия")]
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string? Comission { get; set; }
    }
}
