namespace DataAbstraction.Models.MoneyByMonth
{
	public class BankDepoDBModel : BankDepoDBBaseModel
	{
		public int PlaceName { get; set; }
		public DateTimeOffset DateOpen { get; set; }
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
