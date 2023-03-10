using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Interfaces
{
    public interface IWebDividents
    {
        List<SecCodeAndDividentModel>? GetDividentsTableFromDohod();
        List<SecCodeAndDividentModel>? GetDividentsTableFromInvLab();
        List<SecCodeAndDividentModel> ? GetDividentsTableFromSmartLab();
    }
}
