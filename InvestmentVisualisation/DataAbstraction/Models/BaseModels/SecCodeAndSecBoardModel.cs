using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.BaseModels
{
    public class SecCodeAndSecBoardModel
    {
        [Display(Name = "Тикер")]
        public string SecCode { get; set; }

        [Display(Name = "SecBoard")]
        public int SecBoard { get; set; }
    }
}
