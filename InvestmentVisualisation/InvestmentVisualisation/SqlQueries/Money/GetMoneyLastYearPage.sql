SELECT * 
	FROM money_by_month
        having  `event_date` >= adddate(sysdate(), INTERVAL - 1 year)
			and `event_date` <= sysdate()
	order by `event_date` desc;