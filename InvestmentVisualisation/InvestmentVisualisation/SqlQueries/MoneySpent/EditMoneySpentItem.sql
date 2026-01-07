UPDATE `money_spent_by_month` 
	SET 
		`total` = @total,
		`appartment` = @appartment, 
		`electricity` = @electricity, 
		`internet` = @internet, 
		`phone` = @phone,
		`transport` = @transport,
		`supermarket` = @supermarket,
		`marketplaces` = @marketplaces
	WHERE (`event_date` = @date);