using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Models.YearView
{
	public class NameAndPiecesAndValueModel : SecCodeAndNameAndPiecesModel
	{
		public string FullName { get; set; }
		public decimal Volume { get; set; }
	}
}
