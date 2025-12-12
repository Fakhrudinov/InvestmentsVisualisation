namespace DataAbstraction.Models.MoneyByMonth
{
	public class DayAndVolumeAndNameModel : DecimalVolumeAndNameModel
	{
		public DateTime ? CouponDate {  get; set; }
		public string SecCode { get; set; }
	}
}
