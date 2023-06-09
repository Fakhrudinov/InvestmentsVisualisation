﻿using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.BaseModels
{
    public class BaseModel : SecCodeAndSecBoardModel
    {
        [Display(Name = "Дата")]
        public DateTime Date { get; set; }

        [Display(Name = "Комиссия")]
        [RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
        public string? Comission { get; set; }
    }
}
