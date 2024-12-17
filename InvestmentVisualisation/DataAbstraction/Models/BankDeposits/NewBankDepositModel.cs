using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.BankDeposits
{
	public class NewBankDepositModel
	{
		[Display(Name = "Дата открытия")]
		public DateTime DateOpen { get; set; }
		[Display(Name = "Дата окончания")]
		public DateTime DateClose { get; set; }

		[Display(Name = "Наименование вклада")]
		public string Name { get; set; }

		[Display(Name = "Площадка")]
		public int PlaceNameSign { get; set; }

		[Display(Name = "Процент"), 
			RegularExpression("^(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", 
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string Percent { get; set; }

		[Display(Name = "Объем вклада"),
			RegularExpression("^(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string Summ { get; set; }
	}
}
