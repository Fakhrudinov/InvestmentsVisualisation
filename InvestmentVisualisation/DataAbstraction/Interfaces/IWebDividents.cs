﻿using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.MoneyByMonth;

namespace DataAbstraction.Interfaces
{
    public interface IWebDividents
    {
        DohodDivsAndDatesModel? GetDividentsTableFromDohod(CancellationToken cancellationToken);
        List<SecCodeAndDividentModel> ? GetDividentsTableFromSmartLab(CancellationToken cancellationToken);
        List<SecCodeAndDividentModel>? GetDividentsTableFromVsdelke(CancellationToken cancellationToken);
		List<ExpectedDividentsFromWebModel>? GetFutureDividentsTableFromDohod(CancellationToken cancellationToken);
	}
}
