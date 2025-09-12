UPDATE seccode_info 
	SET 
		`secboard` = @secboard, 
		`name` = @name, 
		`full_name` = @full_name, 
		`isin` = @isin, 
		`expired_date` = @expired_date,
		`payments_per_year` = @payments_per_year
	WHERE (`seccode` = @seccode);
