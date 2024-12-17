using System.ComponentModel.DataAnnotations;

namespace DataAbstraction.Models.BankDeposits
{
	public class BankDepositModel : NewBankDepositModel
	{
		public int Id { get; set; }

		[Display(Name = "Вклад активен")]
		public bool IsOpen { get; set; } = true;

		[Display(Name = "Доход"),
			RegularExpression("^(0|[1-9]\\d{0,7})([.|,]\\d{1,6})?$",
			ErrorMessage = "Только цифры. Разделитель - точка или запятая.")]
		public string? SummIncome { get; set; }
	}
}
