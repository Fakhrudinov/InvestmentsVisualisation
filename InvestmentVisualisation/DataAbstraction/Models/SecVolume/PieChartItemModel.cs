using System.Runtime.Serialization;

namespace DataAbstraction.Models.SecVolume
{
	public class PieChartItemModel
	{
		public PieChartItemModel(string name, decimal y)
		{
			this.name = name;
			this.y = y;
		}
		public PieChartItemModel(string name, decimal y, decimal percent)
		{
			this.name = name;
			this.y = y;
			this.percent = percent;
		}
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "name")]
		public string name;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public decimal y;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "percent")]
		public decimal percent = 0;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "description")]
		public string description;
	}
}
