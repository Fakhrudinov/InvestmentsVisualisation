UPDATE 
		wish_list
	SET 
		wish_level = @level,
		description = '@description'
	WHERE (seccode = '@seccode');