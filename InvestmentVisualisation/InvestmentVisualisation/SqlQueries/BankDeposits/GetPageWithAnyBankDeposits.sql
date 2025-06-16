SELECT id, isopen, event_date, date_close, name, placed_name, percent, summ, income_summ FROM bank_deposits 
	ORDER BY id desc
	LIMIT @lines_count OFFSET @page_number;