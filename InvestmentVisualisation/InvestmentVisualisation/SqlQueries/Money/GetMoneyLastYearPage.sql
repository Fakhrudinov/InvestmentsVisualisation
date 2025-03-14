SELECT * 
	FROM money_by_month
        having  `date_year_month` >= adddate(sysdate(), INTERVAL - 1 year)
			and `date_year_month` <= sysdate()
	order by `date_year_month` desc;