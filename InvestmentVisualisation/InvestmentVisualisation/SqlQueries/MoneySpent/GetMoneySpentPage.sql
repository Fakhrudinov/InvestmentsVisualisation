SELECT `date_year_month`, `total`, `appartment`, `electricity`, `internet`, `phone` 
	FROM money_spent_by_month
	ORDER BY date_year_month desc
LIMIT @lines_count OFFSET @page_number;