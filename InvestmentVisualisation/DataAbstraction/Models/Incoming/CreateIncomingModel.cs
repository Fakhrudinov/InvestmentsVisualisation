using DataAbstraction.Models.BaseModels;
using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.Incoming
{
    public class CreateIncomingModel : BaseModel
    {
        [Display(Name = "Категория")]
        public int Category { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        [Display(Name = "Сумма")]
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string Value { get; set; }
    }
}

