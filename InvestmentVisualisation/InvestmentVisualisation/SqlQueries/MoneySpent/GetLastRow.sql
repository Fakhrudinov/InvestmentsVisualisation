SELECT `date_year_month`, `internet`, `phone`
	FROM money_spent_by_month
		ORDER BY date_year_month desc
	limit 1;