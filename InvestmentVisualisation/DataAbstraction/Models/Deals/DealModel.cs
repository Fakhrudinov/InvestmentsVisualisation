using DataAbstraction.Models.BaseModels;
using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.Deals
{
    public class DealModel : BaseModel
    {
        public int Id { get; set; }

        [Display(Name = "Средняя цена"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string AvPrice { get; set; }

        [Display(Name = "Кол-во")]
        public int Pieces { get; set; }

        [Display(Name = "НКД"), RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string? NKD { get; set; }
    }
}
