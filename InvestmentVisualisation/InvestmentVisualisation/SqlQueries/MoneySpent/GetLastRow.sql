SELECT `event_date`, `internet`, `phone`
	FROM money_spent_by_month
		ORDER BY event_date desc
	limit 1;