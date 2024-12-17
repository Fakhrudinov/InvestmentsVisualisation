SELECT id, isopen, date_open, date_close, name, placed_name, percent, summ, income_summ FROM bank_deposits 
	where isopen = 1
	ORDER BY date_close
	LIMIT @lines_count OFFSET @page_number;