UPDATE bank_deposits 
	SET `isopen` = '0', 
		`income_summ` = @income_summ
	WHERE(`id` = @id);