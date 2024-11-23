﻿using System.Runtime.Serialization;

namespace DataAbstraction.Models.MoneyByMonth
{
	public class BankDepoChartItemModel
	{
		public BankDepoChartItemModel(
			string name, 
			string color,
			//DataPointsOfBankDepoChartItemModel[] dataPoints,
			List<DataPointsOfBankDepoChartItemModel> dataPoints,
			int lineThickness
			)
		{
			this.name = name;
			this.color = color;
			this.dataPoints = dataPoints;
			this.lineThickness = lineThickness;
		}

		//public BankDepoChartItemModel(
		//	string name,
		//	DataPointsOfBankDepoChartItemModel[] dataPoints
		//	)
		//{
		//	this.name = name;
		//	this.dataPoints = dataPoints;
		//}

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "type")]
		public string type = "line";

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
		public string markerType = "none";

		////Explicitly setting the name to be used while serializing to JSON.
		//[DataMember(Name = "markerSize")]
		//public int markerSize = 30;

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "xValueType")]
		public string xValueType = "dateTime";

		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "xValueFormatString")]
		public string xValueFormatString = "MMM DD YYYY";

		////Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "dataPoints")]
		public List<DataPointsOfBankDepoChartItemModel> dataPoints;

		/*
		type: "line",
		name: "Total Visit",
		lineThickness: 30,
		color: "#F08080",
		markerType: "none",
		xValueType: "dateTime",
		xValueFormatString: "MMM DD YYYY",
		dataPoints: [
			{ x: new Date(2017, 0, 15), y: 20 },
			{ x: new Date(2017, 3, 16), y: 20 }
		]
		*/

	}

	public class DataPointsOfBankDepoChartItemModel
	{
		public DataPointsOfBankDepoChartItemModel(long x, int y, string indexLabel)
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
		public string indexLabel = "";
		//Explicitly setting the name to be used while serializing to JSON.
		[DataMember(Name = "indexLabelFontWeight")]
		public string indexLabelFontWeight = "bold";

		////Explicitly setting the name to be used while serializing to JSON.
		//[DataMember(Name = "indexLabelTextAlign")]
		//public string indexLabelTextAlign = "left";
	}
}