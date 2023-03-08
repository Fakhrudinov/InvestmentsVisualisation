using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Interfaces
{
    public interface ISmartLabDividents
    {
        Task<List<SecCodeAndDividentModel>> GetDividentsTable();
    }
}
