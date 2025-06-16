UPDATE `money_spent_by_month` 
	SET 
		`total` = @total,
		`appartment` = @appartment, 
		`electricity` = @electricity, 
		`internet` = @internet, 
		`phone` = @phone
	WHERE (`event_date` = @date);