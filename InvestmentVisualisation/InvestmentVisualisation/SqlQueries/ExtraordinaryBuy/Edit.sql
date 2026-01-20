UPDATE extraordinary_buy 
	SET 
		`volume` = @volume, 
		`description` = @description
	WHERE (`seccode` = @seccode);
