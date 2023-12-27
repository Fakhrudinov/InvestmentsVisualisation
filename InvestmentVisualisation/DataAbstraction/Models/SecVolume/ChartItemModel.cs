using System.Runtime.Serialization;

namespace DataAbstraction.Models.SecVolume
{
	//DataContract for Serializing Data - required to serve in JSON format
	[DataContract]
	public class ChartItemModel
	{
		public ChartItemModel(string label, decimal y)
		{
			this.Label = label;
			this.Y = y;
		}
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "label")]
		public string Label = "";

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public decimal Y = 0;
	}
}
