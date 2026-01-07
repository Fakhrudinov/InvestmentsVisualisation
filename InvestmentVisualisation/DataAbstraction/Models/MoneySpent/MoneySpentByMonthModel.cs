using System.ComponentModel.DataAnnotations;


namespace DataAbstraction.Models.MoneySpent
{
	public class MoneySpentByMonthModel
	{
		[Display(Name = "Дата")]
		//[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
		//public DateTime Date { get; set; }
		// to avoid setting culture ru-RU set Date as string
		public string Date { get; set; } = string.Empty;

		[Display(Name = "Всего потрачено в месяц"), 
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$", 
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Total { get; set; }

		[Display(Name = "КвартПлата"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Appartment { get; set; }

		[Display(Name = "Электричество"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Electricity { get; set; }

		[Display(Name = "Интернет"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Internet { get; set; }

		[Display(Name = "Телефон"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Phone { get; set; }

		[Display(Name = "Транспорт"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? Transport { get; set; }

		[Display(Name = "Супермаркеты"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? SuperMarkets { get; set; }

		[Display(Name = "Маркетплейсы"),
			RegularExpression("^-?(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? MarketPlaces { get; set; }
	}
}
