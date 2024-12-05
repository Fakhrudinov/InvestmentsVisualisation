namespace DataAbstraction.Models.MoneyByMonth
{
	public class ExpectedDividentsFromWebModel
	{
		public string ? SecCode { get; set; }
		public DateTime Date { get; set; }
		public decimal DividentToOnePiece { get; set; }
		public decimal DividentInPercents { get; set; }
		public bool IsConfirmed { get; set; } = false;
		public int Pieces { get; set; } = 0;
	}
}
