SELECT *, STR_TO_DATE(concat(month,'/',year), '%m/%Y') as date_to_sort 
	FROM money_by_month
        having  date_to_sort >= adddate(sysdate(), INTERVAL - 1 year)
			and date_to_sort <= sysdate()
	order by date_to_sort desc;