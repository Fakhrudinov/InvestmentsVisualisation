SELECT `event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`, `transport`, `supermarket`, `marketplaces`
	FROM money_spent_by_month
	WHERE (`event_date` = @date);