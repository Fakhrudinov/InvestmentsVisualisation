using DataAbstraction.Models.MoneyByMonth;

namespace DataAbstraction.Interfaces
{
    public interface IMySqlMoneyRepository
    {
        void FillFreeMoney(CancellationToken cancellationToken);
        Task<List<MoneyModel>> GetMoneyLastYearPage(CancellationToken cancellationToken);
        Task<List<MoneyModel>> GetMoneyYearPage(CancellationToken cancellationToken, int year);
        Task RecalculateMoney(CancellationToken cancellationToken, string v);
    }
}
