using DataAbstraction.Models.MoneyByMonth;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlMoneyRepository
    {
        void FillFreeMoney();
        Task<List<MoneyModel>> GetMoneyLastYearPage();
        Task<List<MoneyModel>> GetMoneyYearPage(int year);
        Task RecalculateMoney(string v);
    }
}
