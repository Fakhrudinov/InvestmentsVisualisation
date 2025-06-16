SELECT money_sum 
	FROM money_by_month 
		where `event_date` = DATE_FORMAT(current_date, '%Y-%m-01');