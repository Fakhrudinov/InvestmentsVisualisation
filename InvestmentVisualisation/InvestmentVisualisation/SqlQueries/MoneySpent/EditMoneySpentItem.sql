UPDATE `money_spent_by_month` 
	SET 
		`total` = @total,
		`appartment` = @appartment, 
		`electricity` = @electricity, 
		`internet` = @internet, 
		`phone` = @phone
	WHERE (`date_year_month` = @date);