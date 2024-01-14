using DataAbstraction.Models.BaseModels;

namespace DataAbstraction.Interfaces
{
    public interface IWebDividents
    {
        DohodDivsAndDatesModel? GetDividentsTableFromDohod(CancellationToken cancellationToken);
        List<SecCodeAndDividentModel> ? GetDividentsTableFromSmartLab(CancellationToken cancellationToken);
        List<SecCodeAndDividentModel>? GetDividentsTableFromVsdelke(CancellationToken cancellationToken);
    }
}
