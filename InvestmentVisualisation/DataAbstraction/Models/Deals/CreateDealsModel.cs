using DataAbstraction.Models.BaseModels;
using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.Deals
{
    public class CreateDealsModel : BaseModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        [Display(Name = "Средняя цена")] 
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string AvPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        [Display(Name = "Количество")]
        public int Pieces { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        [Display(Name = "НКД")]
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string? NKD { get; set; }
    }
}
