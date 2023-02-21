UPDATE seccode_info 
	SET 
		`secboard` = @secboard, 
		`name` = @name, 
		`full_name` = @full_name, 
		`isin` = @isin, 
		`expired_date` = @expired_date 
	WHERE (`seccode` = @seccode);
