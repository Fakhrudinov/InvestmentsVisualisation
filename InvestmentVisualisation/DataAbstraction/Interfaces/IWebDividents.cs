using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Interfaces
{
    public interface IWebDividents
    {
        DohodDivsAndDatesModel? GetDividentsTableFromDohod();
        List<SecCodeAndDividentModel>? GetDividentsTableFromInvLab();
        List<SecCodeAndDividentModel> ? GetDividentsTableFromSmartLab();
        List<SecCodeAndDividentModel>? GetDividentsTableFromVsdelke();
    }
}
