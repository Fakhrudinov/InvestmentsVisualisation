using System.Runtime.Serialization;

namespace DataAbstraction.Models.MoneyByMonth
{
	public class ChartItemModel
	{
		public ChartItemModel(
			string name,
			string color,
			List<DataPointsOfChartItemModel> dataPoints,
			int lineThickness,
			string type,
			string? markerType = "none"
			)
		{
			this.name = name;
			this.color = color;
			this.dataPoints = dataPoints;
			this.lineThickness = lineThickness;
			this.type = type;
			if ( markerType is not null)
			{
				this.markerType = markerType;
			}
		}

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "type")]
		public string type;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "name")]
		public string name;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "label")]
		public string label = "123fssdf ыва фыва asd f";

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "lineThickness")]
		public int lineThickness = 1;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "color")]
		public string color;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "markerType")]
		public string ? markerType;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "xValueType")]
		public string xValueType = "dateTime";

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "xValueFormatString")]
		public string xValueFormatString = "MMM DD YYYY";

		////Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "dataPoints")]
		public List<DataPointsOfChartItemModel> ? dataPoints;
	}

	public class DataPointsOfChartItemModel
	{
        public DataPointsOfChartItemModel(long x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public DataPointsOfChartItemModel(long x, int y, string indexLabel)
		{
			this.x = x;
			this.y = y;
			this.indexLabel=indexLabel;
		}

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "x")]
		public long x = 0;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public int y = 0;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "indexLabel")]
		public string ? indexLabel;
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "indexLabelFontWeight")]
		public string indexLabelFontWeight = "normal";

		[DataMember(Name = "indexLabelFontSize")]
		public int indexLabelFontSize = 12;
	}

	[DataContract]
	public class ExtendedDataPointsOfChartItemModel
	{
		public ExtendedDataPointsOfChartItemModel(
			long x, // date
			decimal y, // percent
			decimal z, // volume in rubles
			string name, // seccode
			string ? color = null
			)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.name = name;
			this.color = color;
		}

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "x")]
		public long x;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "y")]
		public decimal y;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "z")]
		public decimal z;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "name")]
		public string name;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "color")]
		public string ? color;
	}
}
