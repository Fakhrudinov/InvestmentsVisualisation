using DataAbstraction.Models.BaseModels;
using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.Incoming
{
    public class IncomingModel : BaseModel
    {
        public int Id { get; set; }

        [Display(Name = "Category")]
        public int Category { get; set; }

        [Required]
        [Display(Name = "Сумма")]
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string Value { get; set; }

    }
}
