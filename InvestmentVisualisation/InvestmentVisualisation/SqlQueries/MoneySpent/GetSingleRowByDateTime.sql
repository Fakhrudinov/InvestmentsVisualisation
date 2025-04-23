SELECT `date_year_month`, `total`, `appartment`, `electricity`, `internet`, `phone` 
	FROM money_spent_by_month
	WHERE (`date_year_month` = @date);