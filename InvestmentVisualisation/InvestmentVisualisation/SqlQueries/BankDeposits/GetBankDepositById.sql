SELECT id, isopen, date_open, date_close, name, placed_name, percent, summ, income_summ FROM bank_deposits 
	where id = @id;