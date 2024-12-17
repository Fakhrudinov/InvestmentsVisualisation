UPDATE bank_deposits 
	SET `isopen` = @isopen, 
		`date_open` = @date_open, 
		`date_close` = @date_close, 
		`name` = @name, 
		`placed_name` = @placed_name, 
		`percent` = @percent, 
		`summ` = @summ, 
		`income_summ` = @income_summ 
	WHERE (`id` = @id);