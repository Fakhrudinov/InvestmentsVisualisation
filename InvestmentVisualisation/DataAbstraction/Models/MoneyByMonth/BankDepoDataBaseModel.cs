namespace DataAbstraction.Models.MoneyByMonth
{
	public class BankDepoDataBaseModel
	{
		public string Name { get; set; }
		public int PlaceName { get; set; }
		public DateTimeOffset DateOpen { get; set; }
		public DateTimeOffset DateClose { get; set; }
		public decimal Percent { get; set; }
		public decimal SummAmount { get; set; }
		//public string IndexLabel { get; set; }

		/*
///id int AI PK 
///isopen tinyint 
date_open date 
date_close date 
name varchar(45) NULL
placed_name varchar(45) 
percent decimal(10,2) 
summ decimal(15,2) 
///income_summ decimal(15,2) NULL
		 */

	}
}
