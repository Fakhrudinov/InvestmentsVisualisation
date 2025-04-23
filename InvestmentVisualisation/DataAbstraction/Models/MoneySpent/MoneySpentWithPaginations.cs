using DataAbstraction.Models.MoneySpent;

namespace DataAbstraction.Models.Deals
{
    public class MoneySpentWithPaginations
	{
        public List<MoneySpentByMonthModel>? DataByMonths { get; set; }
        public PaginationPageViewModel PageViewModel { get; set; }
    }
}
