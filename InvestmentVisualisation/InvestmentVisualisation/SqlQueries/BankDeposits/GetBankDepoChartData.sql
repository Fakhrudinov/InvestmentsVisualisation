select bd.name, bd.placed_name, bd.event_date, bd.date_close, bd.summ, bd.percent 
	from bank_deposits bd
		where bd.isopen = 1;