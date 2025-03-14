SELECT money_sum 
	FROM money_by_month 
		where `date_year_month` = DATE_FORMAT(current_date, '%Y-%m-01');